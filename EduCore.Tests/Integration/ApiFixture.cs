using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EduCore.Data;
using EduCore.Enums;
using EduCore.Models;
using EduCore.Security;
using EduCore.Security.Encryption;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Xunit;

namespace EduCore.Tests.Integration;

// The real application, hosted in memory, against a real PostgreSQL in a container: migrations,
// query filters, interceptors, policies and middleware all run as they do in production. Needs
// Docker. One container for the whole collection; each test seeds what it needs on top of the
// two schools below.
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public const string JwtKey = "integration-test-signing-key-at-least-32-bytes!";
    public static readonly byte[] EncryptionKey = RandomNumberGenerator.GetBytes(32);

    public WebApplicationFactory<Program> App { get; private set; } = null!;

    // Sign-in is not under test here (tokens are minted directly), so the cheapest valid hash.
    private static readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> Hasher =
        new(Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Identity.PasswordHasherOptions { IterationCount = 1 }));

    public Guid SchoolA { get; } = Guid.CreateVersion7();
    public Guid SchoolB { get; } = Guid.CreateVersion7();

    public async ValueTask InitializeAsync()
    {
        await _db.StartAsync();

        // Environment, not UseSetting: Program reads several of these before the host is built,
        // which is before a WebApplicationFactory's configuration is applied.
        Set("ConnectionStrings__Default", _db.GetConnectionString());
        Set("Jwt__Key", JwtKey);
        Set("Encryption__Key", Convert.ToBase64String(EncryptionKey));
        Set("MigrateOnStart", "true");
        Set("Email__Provider", "Log");
        Set("Session__Secure", "false");
        Set("ScheduledEmail__Enabled", "false");

        App = new WebApplicationFactory<Program>();
        _ = App.CreateClient(); // starts the app, which migrates the empty database

        await using var db = Database();
        db.Schools.AddRange(
            new School { Id = SchoolA, Name = "School A", Type = SchoolType.HighSchool },
            new School { Id = SchoolB, Name = "School B", Type = SchoolType.HighSchool });
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        await _db.DisposeAsync();
    }

    private static void Set(string name, string value) => Environment.SetEnvironmentVariable(name, value);

    // For seeding: the real model and converters, none of the interceptors (seeding is not a
    // request and has no caller to scope or audit).
    public AppDbContext Database(ICurrentUser? actingAs = null, params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_db.GetConnectionString())
            .AddInterceptors(interceptors)
            .Options;

        return new AppDbContext(options, actingAs ?? new Caller(UserRole.StateAdmin, []), new AesFieldEncryptor(EncryptionKey));
    }

    public async Task<User> UserAsync(UserRole role, Guid? schoolId = null)
    {
        await using var db = Database();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            FirstName = role.ToString(),
            LastName = "Test",
            Email = $"{Guid.NewGuid():N}@educore.test",
            Role = role,
            MustChangePassword = false,
        };
        user.SetPassword(Guid.NewGuid().ToString(), Hasher);
        db.Users.Add(user);

        if (role == UserRole.Directorate)
            db.Directorates.Add(new Directorate { UserId = user.Id, SchoolId = schoolId!.Value });

        await db.SaveChangesAsync();
        return user;
    }

    public async Task<Student> StudentAsync(Guid schoolId, string personalNumber)
    {
        await using var db = Database();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            FirstName = "Pupil",
            LastName = "Test",
            Email = $"{Guid.NewGuid():N}@educore.test",
            Role = UserRole.Student,
            MustChangePassword = false,
        };
        user.SetPassword(Guid.NewGuid().ToString(), Hasher);
        var student = new Student
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            SchoolId = schoolId,
            DateOfBirth = new DateOnly(2012, 3, 4),
            PersonalNumber = personalNumber,
        };
        db.Users.Add(user);
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return student;
    }

    // A session as UserService would issue it -- same claims, key, issuer and audience.
    public static string Token(User user, Guid? schoolId = null, DateTime? issuedAt = null)
    {
        var iat = issuedAt ?? DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(iat).ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        if (schoolId is { } id)
            claims.Add(new Claim(ClaimNames.SchoolId, id.ToString()));

        var token = new JwtSecurityToken(
            issuer: "educore",
            audience: "educore-api",
            claims: claims,
            notBefore: iat.AddSeconds(-1),
            expires: iat.AddMinutes(60),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Bearer header: what an API client sends. No session cookie, so the CSRF check does not apply.
    public HttpClient ClientFor(User user, Guid? schoolId = null)
    {
        var client = App.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(user, schoolId));
        return client;
    }

    public sealed class Caller(UserRole role, IReadOnlyCollection<Guid> schools) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => schools.Count == 1 ? schools.First() : null;
        public IReadOnlyCollection<Guid> SchoolIds => schools;
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => role == UserRole.StateAdmin;
        public bool IsRegionalAdmin => role == UserRole.RegionalAdmin;
        public bool IsDirectorate => role == UserRole.Directorate;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
        public bool IsGuardian => role == UserRole.Guardian;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api against PostgreSQL";
}

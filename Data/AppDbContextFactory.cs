using EduCore.Enums;
using EduCore.Security;
using EduCore.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace EduCore.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // dotnet ef bypasses Program.cs, so load .env here too.
        DotEnvFile.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config["ConnectionStrings:Default"];

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(
            optionsBuilder.Options,
            new DesignTimeCurrentUser(),
            new DesignTimeFieldEncryptor());
    }

    // Minimal stand-ins used only while EF builds the model for migrations.
    // Query filters referencing these are compiled as expressions, not evaluated here.
    private class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public UserRole? Role => null;
        public Guid? SchoolId => null;
        public IReadOnlyCollection<Guid> SchoolIds => Array.Empty<Guid>();
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => false;
        public bool IsTeacher => false;
        public bool IsStudent => false;
        public bool IsGuardian => false;
    }

    private class DesignTimeFieldEncryptor : IFieldEncryptor
    {
        public string Encrypt(string value) => value;
        public string Decrypt(string value) => value;
    }
}
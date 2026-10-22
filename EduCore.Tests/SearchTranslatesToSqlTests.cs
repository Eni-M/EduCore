using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Enums;
using EduCore.Repositories;
using EduCore.Security;
using EduCore.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EduCore.Tests;

// The search predicates are only useful if EF turns them into SQL. If one stops translating,
// EF either throws or silently evaluates it in the client -- which is the very thing pushing
// the filter down was meant to stop, and no fake repository would notice.
//
// ToQueryString compiles the query against the real model and the Npgsql provider without
// opening a connection, so this needs no database.
public class SearchTranslatesToSqlTests
{
    private sealed class NoOneInParticular : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public UserRole? Role => null;
        public Guid? SchoolId => null;
        public IReadOnlyCollection<Guid> SchoolIds => [];
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

    private sealed class PlaintextEncryptor : IFieldEncryptor
    {
        public string Encrypt(string value) => value;
        public string Decrypt(string value) => value;
        public string Seal(string value) => value;
        public bool NeedsReencryption(string? stored) => false;
    }

    private static AppDbContext Context()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // Port 1 refuses immediately, so a query that translated fails fast at connect
            // time instead of sitting in a timeout.
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1")
            .Options;

        return new AppDbContext(options, new NoOneInParticular(), new PlaintextEncryptor());
    }

    private static readonly PaginationQueryDto FirstPage = new(1, 20);

    // Every one of these calls the repository's real query builder and forces EF to compile it.
    // A predicate EF cannot translate throws here rather than in production.
    public static TheoryData<string, Func<AppDbContext, Task>> Searches() => new()
    {
        { "subjects", c => new SubjectRepository(c).GetPagedAsync(FirstPage, "math", default) },
        { "schools", c => new SchoolRepository(c)
            .GetPagedAsync(true, Guid.NewGuid(), null, FirstPage, "gjimnazi", default) },
        { "schools by state", c => new SchoolRepository(c)
            .GetPagedAsync(false, null, Guid.NewGuid(), FirstPage, "prishtin", default) },
        { "directorates", c => new DirectorateRepository(c)
            .GetPagedAsync(Guid.NewGuid(), FirstPage, "ana", default) },
        { "regional admins", c => new RegionalAdminRepository(c)
            .GetPagedAsync(Guid.NewGuid(), FirstPage, "ana", default) },
        { "teachers", c => new TeacherRepository(c)
            .GetPagedAsync(Guid.NewGuid(), FirstPage, "biologji", default) },
    };

    [Theory]
    [MemberData(nameof(Searches))]
    public async Task Search_query_compiles_to_sql(string name, Func<AppDbContext, Task> run)
    {
        await using var context = Context();

        // Nothing is listening, so a query that translated fails at connect time. One EF could
        // not translate fails earlier, and says so in the message.
        var error = await Record.ExceptionAsync(() => run(context));

        Assert.False(
            (error?.Message ?? "").Contains("could not be translated", StringComparison.OrdinalIgnoreCase),
            $"The {name} search no longer translates to SQL: {error?.Message}");
    }

    // The paging helper counts and then takes a page. Both halves have to translate, and the
    // ordering has to survive, or paging silently returns rows in an unstable order.
    [Fact]
    public void Teacher_search_reaches_sql_as_an_ilike_over_name_email_and_subject()
    {
        using var context = Context();

        var sql = context.Teachers
            .Where(t => EF.Functions.ILike(t.User.FirstName, "%ana%")
                        || EF.Functions.ILike(t.User.Email, "%ana%")
                        || t.TeacherSubjects.Any(ts => EF.Functions.ILike(ts.Subject.Name, "%ana%")))
            .OrderBy(t => t.User.LastName).ThenBy(t => t.Id)
            .Skip(0).Take(20)
            .ToQueryString();

        Assert.Contains("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT", sql, StringComparison.OrdinalIgnoreCase);
        // The subject match must be a subquery against the join table, not a client-side pass.
        Assert.Contains("EXISTS", sql, StringComparison.OrdinalIgnoreCase);
    }
}

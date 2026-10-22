using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Enums;
using EduCore.Helpers;
using EduCore.Repositories;
using EduCore.Repositories.Statistics;
using EduCore.Security;
using EduCore.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// /api/statistics/academic/at-risk returned 500 for a ministry-wide caller: the paged query
// ordered by an aggregate after projecting it into a record, which EF cannot translate.
//
// Compiling against the real model and the Npgsql provider is enough to catch that -- no
// database needed, because translation happens before a connection is opened. The port is one
// that refuses immediately, so a query that DID translate fails fast at connect instead.
public class AtRiskTranslatesToSqlTests
{
    private sealed class Ministry : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public UserRole? Role => UserRole.StateAdmin;
        public Guid? SchoolId => null;
        public IReadOnlyCollection<Guid> SchoolIds => [];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => true;
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
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1")
            .Options;

        return new AppDbContext(options, new Ministry(), new PlaintextEncryptor());
    }

    private static AcademicStatisticsRepository Repository(AppDbContext context)
    {
        var weights = new GradingOptions();
        var grading = new GradingCalculator(Options.Create(weights));

        return new AcademicStatisticsRepository(
            context, new MarkRepository(context, grading), grading);
    }

    // Ministry-wide: no school, cross-school true -- exactly the shape the failing request had.
    private static StatisticsQuery Nationwide() => new(
        SchoolId: null,
        CrossSchool: true,
        From: new DateOnly(2026, 9, 1),
        To: new DateOnly(2026, 9, 10),
        AcademicYearId: null,
        GradingPeriodId: null,
        ClassId: null,
        Grade: null,
        SubjectId: null,
        TeacherId: null,
        CityId: null,
        ZoneId: null,
        CountryId: null,
        OriginCityId: null);

    [Fact]
    public async Task The_paged_at_risk_query_translates()
    {
        using var context = Context();

        var error = await Record.ExceptionAsync(() =>
            Repository(context).GetAtRiskPagedAsync(Nationwide(), new PaginationQueryDto(1, 20), default));

        Assert.NotNull(error);
        Assert.DoesNotContain("could not be translated", Flatten(error));

        // The repository runs the page before the count for exactly this reason: the count
        // translates on its own, so if it ran first its connection failure would mask a
        // broken page query and this test would pass while the endpoint returned 500.
        Assert.Contains("Failed to connect", Flatten(error));
    }

    [Fact]
    public async Task The_unpaged_at_risk_query_translates()
    {
        using var context = Context();

        var error = await Record.ExceptionAsync(() =>
            Repository(context).GetAtRiskAsync(Nationwide(), default));

        Assert.NotNull(error);
        Assert.DoesNotContain("could not be translated", Flatten(error));
    }

    private static string Flatten(Exception error)
    {
        var text = string.Empty;

        for (Exception? e = error; e is not null; e = e.InnerException)
            text += e.Message + "\n";

        return text;
    }
}

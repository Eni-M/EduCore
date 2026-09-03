using EduCore.Dtos.Statistics;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Security;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Xunit;

namespace EduCore.Tests;

public class StatisticsScopeResolverTests
{
    private static readonly Guid SchoolA = Guid.NewGuid();
    private static readonly Guid SchoolB = Guid.NewGuid();
    private static readonly Guid CityId = Guid.NewGuid();
    private static readonly Guid StateId = Guid.NewGuid();

    private sealed class FakeCurrentUser(
        UserRole role, Guid? schoolId = null, Guid? cityId = null, Guid? stateId = null) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => schoolId;
        public IReadOnlyCollection<Guid> SchoolIds => schoolId is { } s ? [s] : [];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => cityId;
        public Guid? StateId => stateId;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => role == UserRole.StateAdmin;
        public bool IsRegionalAdmin => role == UserRole.RegionalAdmin;
        public bool IsDirectorate => role == UserRole.Directorate;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
        public bool IsGuardian => role == UserRole.Guardian;
    }

    private sealed class StubPeriodResolver : IStatisticsPeriodResolver
    {
        public Guid? LastSchoolId { get; private set; }

        public Task<(DateOnly From, DateOnly To)> ResolveAsync(
            StatisticsFilter filter, Guid? schoolId, CancellationToken ct)
        {
            LastSchoolId = schoolId;
            return Task.FromResult((new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
        }
    }

    private static StatisticsScopeResolver Resolver(ICurrentUser user, StubPeriodResolver periods) =>
        new(user, periods);

    private static StatisticsScopeResolver Directorate(Guid schoolId, StubPeriodResolver periods) =>
        Resolver(new FakeCurrentUser(UserRole.Directorate, schoolId: schoolId), periods);

    [Fact]
    public async Task Directorate_WithNoSchoolId_IsScopedToTheirOwnSchool()
    {
        var context = await Directorate(SchoolA, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter(), CancellationToken.None);

        Assert.Equal(SchoolA, context.SchoolId);
        Assert.False(context.CrossSchool);
    }

    [Fact]
    public async Task Directorate_AskingForTheirOwnSchool_IsAllowed()
    {
        var context = await Directorate(SchoolA, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { SchoolId = SchoolA }, CancellationToken.None);

        Assert.Equal(SchoolA, context.SchoolId);
    }

    [Fact]
    public async Task Directorate_AskingForAnotherSchool_IsRefused() =>
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Directorate(SchoolA, new StubPeriodResolver())
                .ResolveAsync(new StatisticsFilter { SchoolId = SchoolB }, CancellationToken.None));

    [Fact]
    public async Task RegionalAdmin_IsForcedToTheirCity_AndGoesCrossSchool()
    {
        var context = await Resolver(
                new FakeCurrentUser(UserRole.RegionalAdmin, cityId: CityId), new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { SchoolId = SchoolB, CityId = Guid.NewGuid() },
                CancellationToken.None);

        Assert.Null(context.SchoolId);
        Assert.True(context.CrossSchool);
        Assert.Equal(CityId, context.CityId);
    }

    [Fact]
    public async Task StateAdmin_SeesEverySchoolNationwide_ByDefault()
    {
        var context = await Resolver(
                new FakeCurrentUser(UserRole.StateAdmin, stateId: StateId), new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter(), CancellationToken.None);

        Assert.Null(context.SchoolId);
        Assert.True(context.CrossSchool);
        Assert.Null(context.SchoolStateId);
    }

    [Fact]
    public async Task StateAdmin_CanNarrowToAState()
    {
        var otherState = Guid.NewGuid();

        var context = await Resolver(
                new FakeCurrentUser(UserRole.StateAdmin, stateId: StateId), new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { StateId = otherState }, CancellationToken.None);

        Assert.Equal(otherState, context.SchoolStateId);
    }

    [Fact]
    public async Task ResolvedSchool_IsWhatThePeriodResolverSees()
    {
        var periods = new StubPeriodResolver();

        await Directorate(SchoolA, periods).ResolveAsync(new StatisticsFilter(), CancellationToken.None);

        Assert.Equal(SchoolA, periods.LastSchoolId);
    }

    [Fact]
    public async Task FilterDimensions_AreCarriedThrough()
    {
        var classId = Guid.NewGuid();

        var context = await Directorate(SchoolA, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { ClassId = classId, Grade = 9 }, CancellationToken.None);

        Assert.Equal(classId, context.ClassId);
        Assert.Equal(9, context.Grade);
    }

    [Fact]
    public async Task LocationDimensions_AreCarriedThrough_ForADirectorate()
    {
        var cityId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var countryId = Guid.NewGuid();

        var context = await Directorate(SchoolA, new StubPeriodResolver())
            .ResolveAsync(
                // City/Zone are school location; Country is student origin — separate dimensions.
                new StatisticsFilter { CityId = cityId, ZoneId = zoneId, CountryId = countryId },
                CancellationToken.None);

        Assert.Equal(cityId, context.CityId);
        Assert.Equal(zoneId, context.ZoneId);
        Assert.Equal(countryId, context.CountryId);
    }
}

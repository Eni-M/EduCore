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

    private sealed class FakeCurrentUser(UserRole role, Guid? schoolId) : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => schoolId;
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public bool IsSuperAdmin => role == UserRole.SuperAdmin;
        public bool IsAdmin => role == UserRole.Admin;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
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

    private static StatisticsScopeResolver Resolver(UserRole role, Guid? schoolId, StubPeriodResolver periods) =>
        new(new FakeCurrentUser(role, schoolId), periods);

    [Fact]
    public async Task Admin_WithNoSchoolId_IsScopedToTheirOwnSchool()
    {
        var context = await Resolver(UserRole.Admin, SchoolA, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter(), CancellationToken.None);

        Assert.Equal(SchoolA, context.SchoolId);
        Assert.False(context.CrossSchool);
    }

    [Fact]
    public async Task Admin_AskingForTheirOwnSchool_IsAllowed()
    {
        var context = await Resolver(UserRole.Admin, SchoolA, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { SchoolId = SchoolA }, CancellationToken.None);

        Assert.Equal(SchoolA, context.SchoolId);
    }

    [Fact]
    public async Task Admin_AskingForAnotherSchool_IsRefused() =>
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Resolver(UserRole.Admin, SchoolA, new StubPeriodResolver())
                .ResolveAsync(new StatisticsFilter { SchoolId = SchoolB }, CancellationToken.None));

    [Fact]
    public async Task SuperAdmin_WithNoSchoolId_GetsACrossSchoolContext()
    {
        var context = await Resolver(UserRole.SuperAdmin, null, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter(), CancellationToken.None);

        Assert.Null(context.SchoolId);
        Assert.True(context.CrossSchool);
    }

    [Fact]
    public async Task SuperAdmin_CanPickAnySchool()
    {
        var context = await Resolver(UserRole.SuperAdmin, null, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { SchoolId = SchoolB }, CancellationToken.None);

        Assert.Equal(SchoolB, context.SchoolId);
        Assert.False(context.CrossSchool);
    }

    [Fact]
    public async Task ResolvedSchool_IsWhatThePeriodResolverSees()
    {
        var periods = new StubPeriodResolver();

        await Resolver(UserRole.Admin, SchoolA, periods)
            .ResolveAsync(new StatisticsFilter(), CancellationToken.None);

        Assert.Equal(SchoolA, periods.LastSchoolId);
    }

    [Fact]
    public async Task FilterDimensions_AreCarriedThrough()
    {
        var classId = Guid.NewGuid();

        var context = await Resolver(UserRole.Admin, SchoolA, new StubPeriodResolver())
            .ResolveAsync(new StatisticsFilter { ClassId = classId, Grade = 9 }, CancellationToken.None);

        Assert.Equal(classId, context.ClassId);
        Assert.Equal(9, context.Grade);
    }

    [Fact]
    public async Task LocationDimensions_AreCarriedThrough()
    {
        var cityId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var stateId = Guid.NewGuid();

        var context = await Resolver(UserRole.Admin, SchoolA, new StubPeriodResolver())
            .ResolveAsync(
                new StatisticsFilter { CityId = cityId, ZoneId = zoneId, StateId = stateId },
                CancellationToken.None);

        Assert.Equal(cityId, context.CityId);
        Assert.Equal(zoneId, context.ZoneId);
        Assert.Equal(stateId, context.StateId);
    }
}

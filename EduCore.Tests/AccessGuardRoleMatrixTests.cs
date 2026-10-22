using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using Xunit;

namespace EduCore.Tests;

// Every "may this role do that to this thing" rule that moved into AccessGuard, checked across
// roles and relationships, so a branch dropped in a refactor shows up here rather than in prod.
public class AccessGuardRoleMatrixTests
{
    private static readonly Guid MyCity = Guid.NewGuid(), OtherCity = Guid.NewGuid();
    private static readonly Guid MyState = Guid.NewGuid(), OtherState = Guid.NewGuid();
    private static readonly Guid Me = Guid.NewGuid(), SomeoneElse = Guid.NewGuid();

    private sealed class Caller(UserRole role) : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public UserRole? Role => role;
        public Guid? SchoolId => null;
        public IReadOnlyCollection<Guid> SchoolIds => [];
        public Guid? TeacherId { get; init; }
        public Guid? StudentId => null;
        public Guid? CityId { get; init; }
        public Guid? StateId { get; init; }
        public Guid? GuardianId { get; init; }
        public bool IsStateAdmin => role == UserRole.StateAdmin;
        public bool IsRegionalAdmin => role == UserRole.RegionalAdmin;
        public bool IsDirectorate => role == UserRole.Directorate;
        public bool IsTeacher => role == UserRole.Teacher;
        public bool IsStudent => role == UserRole.Student;
        public bool IsGuardian => role == UserRole.Guardian;
    }

    private static AccessGuard Guard(ICurrentUser user) =>
        new(Stub.Of<IClassRepository>(), Stub.Of<IEnrollmentRepository>(),
            Stub.Of<IClassSubjectTeacherRepository>(), Stub.Of<IGuardianRepository>(),
            Stub.Of<ILocationRepository>((method, args) => method.Name == "GetCityAsync"
                ? Task.FromResult<City?>(new City
                {
                    Id = (Guid)args![0]!,
                    Name = "x",
                    StateId = (Guid)args[0]! == MyCity ? MyState : OtherState
                })
                : null),
            user);

    public static TheoryData<UserRole, Guid?, bool> SchoolManagement() => new()
    {
        { UserRole.RegionalAdmin, MyCity, true },
        { UserRole.RegionalAdmin, OtherCity, false },
        { UserRole.RegionalAdmin, null, false },
        { UserRole.StateAdmin, MyCity, true },
        { UserRole.StateAdmin, OtherCity, false },
        { UserRole.StateAdmin, null, false },
        { UserRole.Directorate, MyCity, false },
        { UserRole.Teacher, MyCity, false },
        { UserRole.Guardian, MyCity, false },
    };

    [Theory]
    [MemberData(nameof(SchoolManagement))]
    public async Task Who_may_manage_a_school(UserRole role, Guid? schoolCity, bool allowed)
    {
        var guard = Guard(new Caller(role) { CityId = MyCity, StateId = MyState });
        var act = () => guard.EnsureCanManageSchoolAsync(schoolCity, CancellationToken.None);

        if (allowed) await act();
        else await Assert.ThrowsAsync<ForbiddenException>(act);
    }

    [Theory]
    [InlineData(UserRole.Directorate, false, true)]
    [InlineData(UserRole.Teacher, true, true)]
    [InlineData(UserRole.Teacher, false, false)]
    [InlineData(UserRole.Student, true, false)]
    [InlineData(UserRole.Guardian, true, false)]
    [InlineData(UserRole.RegionalAdmin, true, false)]
    public void Who_may_write_marks(UserRole role, bool isAssignedTeacher, bool allowed)
    {
        var guard = Guard(new Caller(role) { TeacherId = Me });
        var act = () => guard.EnsureCanWriteMarks(isAssignedTeacher ? Me : SomeoneElse);

        if (allowed) act();
        else Assert.Throws<ForbiddenException>(act);
    }

    [Theory]
    [InlineData(UserRole.Guardian, true, true)]
    [InlineData(UserRole.Guardian, false, false)]
    [InlineData(UserRole.Teacher, true, true)]
    [InlineData(UserRole.Teacher, false, false)]
    [InlineData(UserRole.Directorate, true, false)]
    [InlineData(UserRole.StateAdmin, true, false)]
    public void Who_may_read_a_conversation(UserRole role, bool isParticipant, bool allowed)
    {
        var guard = Guard(new Caller(role) { TeacherId = Me, GuardianId = Me });
        var participant = isParticipant ? Me : SomeoneElse;
        var act = () => guard.EnsureConversationParticipant(participant, participant);

        if (allowed) act();
        else Assert.Throws<ForbiddenException>(act);
    }
}

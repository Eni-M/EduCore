using EduCore.Dtos.StudentHistory;
using EduCore.Enums;
using EduCore.Helpers;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Interfaces;
using EduCore.Services.Statistics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// A transcript is the one screen that has to keep working after a student leaves, a school is
// renamed, and a grading threshold is changed. These cover the properties that follow from that:
// history is assembled across schools the caller has no claim on, rebuilding is idempotent, and
// pass/fail is derived from the current rules rather than frozen into the stored row.
public class StudentAcademicHistoryServiceTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid CurrentSchool = Guid.NewGuid();
    private static readonly Guid PreviousSchool = Guid.NewGuid();
    private static readonly Guid YearOne = Guid.NewGuid();
    private static readonly Guid YearTwo = Guid.NewGuid();
    private static readonly Guid EnrollmentOne = Guid.NewGuid();
    private static readonly Guid EnrollmentTwo = Guid.NewGuid();
    private static readonly Guid Maths = Guid.NewGuid();
    private static readonly Guid HistorySubject = Guid.NewGuid();

    private sealed class FakeAccessGuard : IAccessGuard
    {
        public Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanViewStudentMedicalAsync(Guid studentId, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> CanViewStudentMedicalAsync(Guid studentId, CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => UserRole.Directorate;
        public IReadOnlyCollection<Guid> SchoolIds => [CurrentSchool];
        public Guid? SchoolId => CurrentSchool;
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => true;
        public bool IsTeacher => false;
        public bool IsStudent => false;
        public bool IsGuardian => false;
    }

    // Stands in for the database, keeping written records in a list so a rebuild can be run twice
    // and the result inspected.
    private sealed class FakeRecords(
        List<EnrollmentYearRow> enrollments,
        Dictionary<Guid, List<SubjectPeriodMarkRow>> marks) : IStudentAcademicRecordRepository
    {
        public List<Models.StudentAcademicRecord> Stored { get; } = [];

        public Task<string?> GetStudentNameAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<string?>("Ana Berisha");

        public Task<IReadOnlyList<Models.StudentAcademicRecord>> GetForStudentAsync(
            Guid studentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Models.StudentAcademicRecord>>(
                Stored.OrderByDescending(r => r.EnrolledAt).ToList());

        public Task<IReadOnlyList<EnrollmentYearRow>> GetEnrollmentYearsAsync(
            Guid studentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EnrollmentYearRow>>(enrollments);

        public Task<IReadOnlyList<SubjectPeriodMarkRow>> GetPeriodMarksAsync(
            Guid enrollmentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubjectPeriodMarkRow>>(
                marks.TryGetValue(enrollmentId, out var rows) ? rows : []);

        public void Add(Models.StudentAcademicRecord record) => Stored.Add(record);
        public void Remove(Models.StudentAcademicRecord record) => Stored.Remove(record);
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    // One year at the school the caller belongs to, one at a school they do not.
    private static List<EnrollmentYearRow> TwoSchools() =>
    [
        new(EnrollmentTwo, CurrentSchool, "Naim Frasheri", YearTwo, "2025/2026", 8, "8A",
            new DateOnly(2025, 9, 1), null, null),
        new(EnrollmentOne, PreviousSchool, "Ismail Qemali", YearOne, "2024/2025", 7, "7B",
            new DateOnly(2024, 9, 1), new DateOnly(2025, 6, 15), EnrollmentExitReason.TransferredOut)
    ];

    private static (StudentAcademicHistoryService Service, FakeRecords Records) Build(
        Dictionary<Guid, List<SubjectPeriodMarkRow>>? marks = null,
        int passMark = 2)
    {
        var records = new FakeRecords(TwoSchools(), marks ?? []);

        var grading = new GradingCalculator(Options.Create(new GradingOptions
        {
            FirstMarkWeight = 0.40,
            SecondMarkWeight = 0.60,
            PassMark = passMark,
            AtRiskMark = 2
        }));

        var service = new StudentAcademicHistoryService(
            records, new FakeAccessGuard(), grading, new FakeCurrentUser());

        return (service, records);
    }

    private static async Task<StudentAcademicHistoryDto> CompiledAsync(
        StudentAcademicHistoryService service)
    {
        await service.RebuildAsync(StudentId, CancellationToken.None);
        return await service.GetAsync(StudentId, CancellationToken.None);
    }

    private static StudentAcademicYearDto CurrentYear(StudentAcademicHistoryDto history) =>
        history.Years.Single(y => y.AcademicYearName == "2025/2026");

    [Fact]
    public async Task Rebuild_writes_one_record_per_school_year()
    {
        var (service, records) = Build();

        var years = await service.RebuildAsync(StudentId, CancellationToken.None);

        Assert.Equal(2, years);
        Assert.Equal(2, records.Stored.Count);
    }

    [Fact]
    public async Task Rebuilding_twice_does_not_duplicate_records()
    {
        // Why RebuildAsync matches on (SchoolId, AcademicYearId) rather than always inserting:
        // it is expected to be re-run every time marks change.
        var (service, records) = Build();

        await service.RebuildAsync(StudentId, CancellationToken.None);
        await service.RebuildAsync(StudentId, CancellationToken.None);

        Assert.Equal(2, records.Stored.Count);
    }

    [Fact]
    public async Task History_includes_a_school_the_caller_has_no_claim_on()
    {
        // The entire purpose of the table: the previous school is not in SchoolIds, so every
        // school-scoped query in the application would hide this year.
        var (service, _) = Build();

        var history = await CompiledAsync(service);

        Assert.Equal(2, history.Years.Count);
        Assert.Contains(history.Years, y => y.SchoolName == "Ismail Qemali" && !y.IsCurrentSchool);
        Assert.Contains(history.Years, y => y.SchoolName == "Naim Frasheri" && y.IsCurrentSchool);
    }

    [Fact]
    public async Task Newest_year_comes_first()
    {
        var (service, _) = Build();

        var history = await CompiledAsync(service);

        Assert.Equal("2025/2026", history.Years[0].AcademicYearName);
    }

    [Fact]
    public async Task The_exit_reason_and_leaving_date_are_carried_through()
    {
        var (service, _) = Build();

        var history = await CompiledAsync(service);
        var previous = history.Years.Single(y => y.SchoolName == "Ismail Qemali");

        Assert.Equal(EnrollmentExitReason.TransferredOut, previous.ExitReason);
        Assert.Equal(new DateOnly(2025, 6, 15), previous.LeftAt);
    }

    [Fact]
    public async Task An_annual_mark_averages_the_period_finals()
    {
        // Two grading periods for one subject. 4/5 weights to 4.6 and rounds to 5; 3/3 gives 3.
        // The annual mark is the average of those two finals, 4.
        var (service, _) = Build(new Dictionary<Guid, List<SubjectPeriodMarkRow>>
        {
            [EnrollmentTwo] =
            [
                new(Maths, "Mathematics", 4, 5),
                new(Maths, "Mathematics", 3, 3)
            ]
        });

        var history = await CompiledAsync(service);
        var subject = CurrentYear(history).Subjects.Single();

        Assert.Equal("Mathematics", subject.SubjectName);
        Assert.Equal(4, subject.AnnualMark);
    }

    [Fact]
    public async Task Pass_and_fail_follow_the_current_threshold_not_the_stored_row()
    {
        // The same stored mark under two configured pass marks. Storing "passed" would freeze the
        // first answer and put every historical record permanently out of step with the rules.
        var marks = new Dictionary<Guid, List<SubjectPeriodMarkRow>>
        {
            [EnrollmentTwo] = [new(Maths, "Mathematics", 3, 3)]
        };

        var (lenient, _) = Build(marks, passMark: 2);
        var (strict, _) = Build(marks, passMark: 4);

        Assert.True(CurrentYear(await CompiledAsync(lenient)).Subjects.Single().Passed);
        Assert.False(CurrentYear(await CompiledAsync(strict)).Subjects.Single().Passed);
    }

    [Fact]
    public async Task An_ungraded_subject_is_neither_passed_nor_failed()
    {
        // Null rather than false: "no mark recorded" and "failed" are different facts, and a
        // transcript that conflates them accuses a student of failing a subject nobody graded.
        var (service, _) = Build(new Dictionary<Guid, List<SubjectPeriodMarkRow>>
        {
            [EnrollmentTwo] = [new(HistorySubject, "History", null, null)]
        });

        var year = CurrentYear(await CompiledAsync(service));

        Assert.Null(year.Subjects.Single().Passed);
        Assert.Equal(1, year.SubjectsTaken);
        Assert.Equal(0, year.SubjectsGraded);
        Assert.Null(year.AverageMark);
    }

    [Fact]
    public async Task Counts_and_average_are_derived_from_the_graded_subjects_only()
    {
        var (service, _) = Build(new Dictionary<Guid, List<SubjectPeriodMarkRow>>
        {
            [EnrollmentTwo] =
            [
                new(Maths, "Mathematics", 5, 5),
                new(HistorySubject, "History", 1, 1)
            ]
        });

        var year = CurrentYear(await CompiledAsync(service));

        Assert.Equal(2, year.SubjectsGraded);
        Assert.Equal(1, year.SubjectsPassed);
        Assert.Equal(1, year.SubjectsFailed);
        Assert.Equal(3.0, year.AverageMark);
    }
}

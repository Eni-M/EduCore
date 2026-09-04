using System.Diagnostics;
using System.Text;
using EduCore.Data;
using EduCore.Dtos.Pagination;
using EduCore.Dtos.Statistics;
using EduCore.Dtos.Statistics.Aggregate;
using EduCore.Dtos.Statistics.Individual;
using EduCore.Enums;
using EduCore.Helpers;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Repositories.Statistics;
using EduCore.Repositories.Statistics.Interfaces;
using EduCore.Security;
using EduCore.Security.Encryption;
using EduCore.Services.Statistics;
using EduCore.Services.Statistics.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

// TEMPORARY wave-2 verification (live dev DB). Old-vs-new equivalence for at-risk and
// excessive-absences paging, plus a transaction-rollback fixture with crafted ties and
// threshold-boundary rows. rolled back, then verified absent. Deleted after verification.
public sealed class TempWave2VerificationTests
{
    private static readonly string EvidencePath =
        "C:\\Users\\enim\\AppData\\Local\\Temp\\opencode\\perf-wave2.txt";

    private sealed class FakeUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
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

    private sealed class IdentityEncryptor : IFieldEncryptor
    {
        public string Encrypt(string plaintext) => plaintext;
        public string Decrypt(string stored) => stored;
    }

    private sealed class ThrowingMarkRepo : IMarkRepository
    {
        public IQueryable<double> WeightedValues(IQueryable<Mark> marks) =>
            throw new NotImplementedException();
        public IQueryable<Rows.ValuedMark> Value(IQueryable<Mark> marks) =>
            throw new NotImplementedException();
        public int? FinalMark(int? firstMark, int? secondMark) => throw new NotImplementedException();
        public Task<IEnumerable<Dtos.Mark.MarkListDto>> GetByStudentAsync(
            Guid a, Guid? b, Guid? c, CancellationToken d) => throw new NotImplementedException();
        public Task<IEnumerable<Dtos.Mark.MarkListDto>> GetByClassAsync(
            Guid a, Guid? b, Guid? c, CancellationToken d) => throw new NotImplementedException();
        public Task<Dtos.Mark.MarkListDto?> GetDtoAsync(Guid a, CancellationToken b) =>
            throw new NotImplementedException();
        public Task<Mark?> GetForUpsertAsync(Guid a, Guid b, Guid c, CancellationToken d) =>
            throw new NotImplementedException();
        public Task<Mark?> GetByIdAsync(Guid a, CancellationToken b) => throw new NotImplementedException();
        public Task AddAsync(Mark a) => throw new NotImplementedException();
        public void Update(Mark a) => throw new NotImplementedException();
        public void Remove(Mark a) => throw new NotImplementedException();
        public Task<bool> SaveChangesAsync(CancellationToken a) => throw new NotImplementedException();
    }

    private sealed class PassThroughSuppressor : ICellSuppressor
    {
        public Measure Apply(Measure measure) => measure;
        public IReadOnlyList<Measure> Apply(IEnumerable<Measure> measures) => measures.ToList();
        public bool ShouldSuppress(int sampleSize) => false;
    }

    private sealed class StubScope(StatisticsQuery ctx) : IStatisticsScopeResolver
    {
        public Task<StatisticsQuery> ResolveAsync(StatisticsFilter filter, CancellationToken ct) =>
            Task.FromResult(ctx);
    }

    private static readonly GradingOptions Weights = new()
    {
        FirstMarkWeight = 0.4,
        SecondMarkWeight = 0.6,
        PassMark = 2,
        AtRiskMark = 2,
    };

    private static string ConnectionString()
    {
        var candidates = new List<string>();
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            candidates.Add(Path.Combine(dir.FullName, ".env"));
            dir = dir.Parent;
        }
        candidates.Add("C:\\Users\\enim\\EduCore\\.env");

        foreach (var candidate in candidates.Distinct())
        {
            if (!File.Exists(candidate))
                continue;
            foreach (var line in File.ReadAllLines(candidate))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("ConnectionStrings__Default=", StringComparison.Ordinal))
                {
                    var value = trimmed["ConnectionStrings__Default=".Length..].Trim().Trim('"');
                    if (value.Length > 0)
                        return value;
                }
            }
        }

        throw new InvalidOperationException("ConnectionStrings__Default not found in .env.");
    }

    private static AppDbContext CreateContext(List<string> sql, string cs) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs, o => o.CommandTimeout(180))
            .LogTo(sql.Add, [RelationalEventId.CommandExecuted])
            .Options,
            new FakeUser(),
            new IdentityEncryptor());

    private static AcademicStatisticsService AcademicService(AppDbContext context, StatisticsQuery ctx) =>
        new(new AcademicStatisticsRepository(
                context, new ThrowingMarkRepo(), new GradingCalculator(Options.Create(Weights))),
            new StubScope(ctx),
            new PassThroughSuppressor());

    private static AttendanceStatisticsService AttendanceService(AppDbContext context, StatisticsQuery ctx) =>
        new(new AttendanceStatisticsRepository(context),
            new StubScope(ctx),
            new PassThroughSuppressor());

    private async Task<Guid> AnySchoolIdAsync(string cs)
    {
        await using var context = CreateContext([], cs);
        return await context.Schools.IgnoreQueryFilters()
            .OrderBy(s => s.Name).Select(s => s.Id).FirstAsync();
    }

    private async Task RunAtRiskScenarioAsync(string name, StatisticsQuery ctx, int pageSize)
    {
        var cs = ConnectionString();
        var sb = new StringBuilder();
        sb.AppendLine($"===== at-risk {name} =====");

        List<AtRiskStudentDto> oldFull;
        long oldMs;
        {
            await using var context = CreateContext([], cs);
            var sw = Stopwatch.StartNew();
            oldFull = (await AcademicService(context, ctx)
                .GetAtRiskAsync(new StatisticsFilter(), CancellationToken.None)).ToList();
            sw.Stop();
            oldMs = sw.ElapsedMilliseconds;
        }
        sb.AppendLine($"OLD: {oldFull.Count} students, {oldMs} ms");

        var allNew = new List<AtRiskStudentDto>();
        PagedResultDto<AtRiskStudentDto>? first = null;
        var newSql = new List<string>();
        {
            await using var context = CreateContext(newSql, cs);
            var service = AcademicService(context, ctx);
            var page = 1;
            while (true)
            {
                var paged = await service.GetAtRiskPagedAsync(
                    new StatisticsFilter(), new PaginationQueryDto(page, pageSize),
                    CancellationToken.None);
                first ??= paged;
                allNew.AddRange(paged.Items);
                if (page * paged.PageSize >= paged.TotalCount)
                    break;
                page++;
            }
        }
        sb.AppendLine($"NEW: {allNew.Count} students, total={first!.TotalCount}, " +
                      $"{newSql.Count} statements for all pages");

        PagedResultDto<AtRiskStudentDto> beyond;
        {
            await using var context = CreateContext([], cs);
            var service = AcademicService(context, ctx);
            var totalPages = first.PageSize == 0
                ? 0 : (int)Math.Ceiling(first.TotalCount / (double)first.PageSize);
            beyond = await service.GetAtRiskPagedAsync(
                new StatisticsFilter(), new PaginationQueryDto(totalPages + 1, pageSize),
                CancellationToken.None);
        }

        static List<AtRiskStudentDto> Canon(List<AtRiskStudentDto> xs) =>
            xs.OrderBy(c => c.AverageFinalMark).ThenBy(c => c.StudentId).ToList();

        Assert.Equal(oldFull.Count, first.TotalCount);
        Assert.Equal(Canon(oldFull), Canon(allNew));
        Assert.Empty(beyond.Items);
        Assert.Equal(oldFull.Count, beyond.TotalCount);

        var allNewSql = string.Join("\n", newSql);
        Assert.Contains("LIMIT", allNewSql);

        sb.AppendLine("ASSERTIONS: totals, concatenated pages, out-of-range page identical; " +
                      "new SQL uses LIMIT.");
        File.AppendAllText(EvidencePath, sb.ToString());
    }

    private async Task RunExcessiveScenarioAsync(
        string name, StatisticsQuery ctx, double? threshold, int pageSize)
    {
        var cs = ConnectionString();
        var sb = new StringBuilder();
        sb.AppendLine($"===== excessive {name} (threshold={(threshold?.ToString() ?? "default")}) =====");

        List<ExcessiveAbsenceStudentDto> oldFull;
        {
            await using var context = CreateContext([], cs);
            oldFull = (await AttendanceService(context, ctx)
                .GetExcessiveAbsencesAsync(new StatisticsFilter(), threshold, CancellationToken.None))
                .ToList();
        }
        sb.AppendLine($"OLD: {oldFull.Count} students");

        var allNew = new List<ExcessiveAbsenceStudentDto>();
        PagedResultDto<ExcessiveAbsenceStudentDto>? first = null;
        var newSql = new List<string>();
        {
            await using var context = CreateContext(newSql, cs);
            var service = AttendanceService(context, ctx);
            var page = 1;
            while (true)
            {
                var paged = await service.GetExcessiveAbsencesPagedAsync(
                    new StatisticsFilter(), threshold, new PaginationQueryDto(page, pageSize),
                    CancellationToken.None);
                first ??= paged;
                allNew.AddRange(paged.Items);
                if (page * paged.PageSize >= paged.TotalCount)
                    break;
                page++;
            }
        }
        sb.AppendLine($"NEW: {allNew.Count} students, total={first!.TotalCount}");

        static List<ExcessiveAbsenceStudentDto> Canon(List<ExcessiveAbsenceStudentDto> xs) =>
            xs.OrderByDescending(c => (c.Absences - c.ExcusedAbsences) * 100.0 / c.LessonsRecorded)
                .ThenBy(c => c.StudentId).ThenBy(c => c.ClassId).ToList();

        Assert.Equal(oldFull.Count, first.TotalCount);
        Assert.Equal(Canon(oldFull), Canon(allNew));

        var allNewSql = string.Join("\n", newSql);
        Assert.Contains("LIMIT", allNewSql);
        Assert.Contains("round(", allNewSql);

        sb.AppendLine("ASSERTIONS: totals and concatenated pages identical; new SQL uses " +
                      "LIMIT with translated round().");
        sb.AppendLine("----- NEW first-page SQL (first 3 statements) -----");
        var shown = 0;
        var current = new StringBuilder();
        foreach (var line in newSql.SelectMany(s => s.Split('\n')))
        {
            current.AppendLine(line);
            if (line.Contains("Executed DbCommand"))
            {
                if (++shown > 3)
                    break;
                sb.Append(current.ToString());
                current.Clear();
            }
        }
        sb.AppendLine();
        File.AppendAllText(EvidencePath, sb.ToString());
    }

    [Fact]
    public async Task Verify_AtRisk_LiveData()
    {
        var cs = ConnectionString();
        var schoolId = await AnySchoolIdAsync(cs);
        var wide = (Guid? school, bool cross, int? grade) => new StatisticsQuery(
            school, cross, new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31),
            null, null, null, grade, null, null, null, null, null, null, null);

        await RunAtRiskScenarioAsync("school", wide(schoolId, false, null), 20);
        await RunAtRiskScenarioAsync("cross-school", wide(null, true, null), 100);
        await RunAtRiskScenarioAsync("grade", wide(null, true, 5), 20);
    }

    [Fact]
    public async Task Verify_Excessive_LiveData()
    {
        var cs = ConnectionString();
        var schoolId = await AnySchoolIdAsync(cs);
        var wide = (Guid? school, bool cross) => new StatisticsQuery(
            school, cross, new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31),
            null, null, null, null, null, null, null, null, null, null, null);

        await RunExcessiveScenarioAsync("school", wide(schoolId, false), null, 20);
        await RunExcessiveScenarioAsync("cross-school", wide(null, true), 20.0, 100);
    }

    [Fact]
    public async Task Verify_Fixture_TiesAndBoundaries()
    {
        var cs = ConnectionString();
        var schoolId = await AnySchoolIdAsync(cs);
        var sb = new StringBuilder();
        sb.AppendLine("===== fixture: ties + threshold boundary (rolled back) =====");

        var ayId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var subjectBId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var cstId = Guid.NewGuid();
        var cstBId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var studentIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        var userIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        var enrollmentIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();

        // present/absent-unexcused/absent-excused per student over 10 lessons:
        // S1 0/10/0 (100%), S2 8/2/0 (20% boundary, included), S3 8/1/1 (10%, excluded),
        // S4 0/10/0 (100% tie with S1), S5 6/2/2 (20% included, DTO rate 40%).
        var attendancePlan = new (int Present, int Unexcused, int Excused)[]
        {
            (0, 10, 0), (8, 2, 0), (8, 1, 1), (0, 10, 0), (6, 2, 2),
        };
        // weighted averages with 0.4/0.6: S1 2.0 (in), S2 3.0 (out), S3 1.0 (in),
        // S4 1.0 (tie with S3), S5 none (absent). Second marks use subject B: marks are
        // unique per (enrollment, subject, period).
        var markPlan = new (int First, int Second, bool SecondSubject)[][]
        {
            [(2, 2, false), (2, 2, true)],
            [(3, 3, false), (3, 3, true)],
            [(1, 1, false)],
            [(1, 1, false)],
            [],
        };

        var ctx = new StatisticsQuery(schoolId, false,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            null, null, null, null, null, null, null, null, null, null, null);

        PagedResultDto<ExcessiveAbsenceStudentDto> excDefault;
        PagedResultDto<ExcessiveAbsenceStudentDto> excExplicit;
        PagedResultDto<AtRiskStudentDto> riskPage1;
        PagedResultDto<AtRiskStudentDto> riskPage2;
        List<ExcessiveAbsenceStudentDto> excOld;
        List<AtRiskStudentDto> riskOld;

        var sql = new List<string>();
        await using (var context = CreateContext(sql, cs))
        {
            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                context.Add(new AcademicYear
                {
                    Id = ayId, SchoolId = schoolId, Name = "TX Year",
                    StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2026, 8, 31),
                });
                context.Add(new Class
                {
                    Id = classId, SchoolId = schoolId, AcademicYearId = ayId,
                    Name = "TX Class", Grade = 5,
                });
                context.Add(new Subject { Id = subjectId, SchoolId = schoolId, Name = "TX Subject" });
                context.Add(new Subject { Id = subjectBId, SchoolId = schoolId, Name = "TX Subject B" });
                context.Add(NewUser(teacherUserId, "TX", "Teacher", "tx-teacher@tx.test", UserRole.Teacher));
                context.Add(new Teacher { Id = teacherId, UserId = teacherUserId, SchoolId = schoolId });
                context.Add(new ClassSubjectTeacher
                {
                    Id = cstId, ClassId = classId, SubjectId = subjectId,
                    TeacherId = teacherId, SchoolId = schoolId,
                });
                context.Add(new ClassSubjectTeacher
                {
                    Id = cstBId, ClassId = classId, SubjectId = subjectBId,
                    TeacherId = teacherId, SchoolId = schoolId,
                });
                context.Add(new GradingPeriod
                {
                    Id = periodId, AcademicYearId = ayId, SchoolId = schoolId, Name = "TX Term",
                    StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
                });

                for (var i = 0; i < 5; i++)
                {
                    context.Add(NewUser(userIds[i], $"TXS{i + 1}", "Pupil",
                        $"tx-s{i + 1}@tx.test", UserRole.Student));
                    context.Add(new Student
                    {
                        Id = studentIds[i], UserId = userIds[i], SchoolId = schoolId,
                        DateOfBirth = new DateOnly(2014, 6, 1), PersonalNumber = $"TX-P00{i + 1}",
                    });
                    context.Add(new Enrollment
                    {
                        Id = enrollmentIds[i], StudentId = studentIds[i], ClassId = classId,
                        SchoolId = schoolId, EnrolledAt = new DateOnly(2025, 9, 15),
                    });
                }

                var diaryIds = new List<Guid>();
                for (var d = 0; d < 10; d++)
                {
                    var id = Guid.NewGuid();
                    diaryIds.Add(id);
                    context.Add(new DiaryEntry
                    {
                        Id = id, ClassId = classId, SubjectId = subjectId, TeacherId = teacherId,
                        SchoolId = schoolId, Date = new DateOnly(2026, 2, 2).AddDays(d),
                        Period = 1, Topic = $"TX lesson {d}",
                    });
                }

                for (var i = 0; i < 5; i++)
                {
                    var (present, unexcused, excused) = attendancePlan[i];
                    var lesson = 0;
                    void AddRows(int count, bool isPresent, bool isExcused)
                    {
                        for (var k = 0; k < count; k++)
                        {
                            var row = new Attendance
                            {
                                Id = Guid.NewGuid(), DiaryEntryId = diaryIds[lesson++],
                                EnrollmentId = enrollmentIds[i], ClassId = classId, SchoolId = schoolId,
                            };
                            row.Record(isPresent, isExcused);
                            context.Add(row);
                        }
                    }
                    AddRows(present, true, false);
                    AddRows(unexcused, false, false);
                    AddRows(excused, false, true);

                    foreach (var (first, second, secondSubject) in markPlan[i])
                    {
                        context.Add(new Mark
                        {
                            Id = Guid.NewGuid(), EnrollmentId = enrollmentIds[i], ClassId = classId,
                            AcademicYearId = ayId, SchoolId = schoolId,
                            ClassSubjectTeacherId = secondSubject ? cstBId : cstId,
                            SubjectId = secondSubject ? subjectBId : subjectId,
                            TeacherId = teacherId, GradingPeriodId = periodId,
                            FirstMark = first, SecondMark = second,
                        });
                    }
                }

                await context.SaveChangesAsync();

                var attendance = AttendanceService(context, ctx);
                var academic = AcademicService(context, ctx);

                var filter = new StatisticsFilter { SchoolId = schoolId };
                excDefault = await attendance.GetExcessiveAbsencesPagedAsync(
                    filter, null, new PaginationQueryDto(1, 2), CancellationToken.None);
                excExplicit = await attendance.GetExcessiveAbsencesPagedAsync(
                    filter, 20.0, new PaginationQueryDto(2, 2), CancellationToken.None);
                riskPage1 = await academic.GetAtRiskPagedAsync(
                    filter, new PaginationQueryDto(1, 2), CancellationToken.None);
                riskPage2 = await academic.GetAtRiskPagedAsync(
                    filter, new PaginationQueryDto(2, 2), CancellationToken.None);
                excOld = (await attendance.GetExcessiveAbsencesAsync(
                    filter, null, CancellationToken.None)).ToList();
                riskOld = (await academic.GetAtRiskAsync(filter, CancellationToken.None)).ToList();
            }
            finally
            {
                await tx.RollbackAsync();
            }
        }

        // Excessive expectations: S1+S4 at 100%, S2+S5 at 20% (boundary included),
        // S3 at 10% excluded. Ties broken by student id.
        var byId = studentIds
            .Select((id, i) => (Id: id, Index: i))
            .OrderByDescending(x => attendancePlan[x.Index].Unexcused * 100.0 / 10)
            .ThenBy(x => x.Id)
            .ToList();
        var included = byId.Where(x => x.Index != 2).ToList();
        Assert.Equal(4, included.Count);

        ExcessiveAbsenceStudentDto Dto(int i) => new(
            studentIds[i], $"TXS{i + 1} Pupil", classId, "TX Class", 10,
            10 - attendancePlan[i].Present, attendancePlan[i].Excused,
            Math.Round((10 - attendancePlan[i].Present) * 100.0 / 10, 2));

        var expectedPage1 = new[] { Dto(included[0].Index), Dto(included[1].Index) };
        var expectedPage2 = new[] { Dto(included[2].Index), Dto(included[3].Index) };

        Assert.Equal(4, excDefault.TotalCount);
        Assert.Equal(expectedPage1, excDefault.Items.ToList());
        Assert.Equal(expectedPage2, excExplicit.Items.ToList());
        Assert.Equal(
            included.Select(x => Dto(x.Index)).OrderBy(d => d.StudentId).ToList(),
            excOld.OrderBy(d => d.StudentId).ToList());

        // At-risk expectations: S3+S4 at 1.0 (tie), S1 at 2.0; S2 at 3.0 excluded,
        // S5 without marks absent.
        AtRiskStudentDto Risk(int i, double avg, int failed, int atRisk) => new(
            studentIds[i], $"TXS{i + 1} Pupil", classId, "TX Class", 5, avg, failed, atRisk);

        var first = new[] { studentIds[2], studentIds[3] }.Order().ToArray();
        var expectedRisk1 = first.Select(id => id == studentIds[2]
            ? Risk(2, 1.0, 1, 0) : Risk(3, 1.0, 1, 0)).ToArray();

        Assert.Equal(3, riskPage1.TotalCount);
        Assert.Equal(expectedRisk1, riskPage1.Items.ToArray());
        Assert.Equal([Risk(0, 2.0, 0, 2)], riskPage2.Items.ToArray());
        Assert.Equal(3, riskOld.Count);

        sb.AppendLine("ASSERTIONS: excessive pages exact incl. 20%-boundary inclusion, " +
                      "100%-tie order, dual-rate DTOs; at-risk pages exact incl. 1.0-tie; " +
                      "old-vs-new multisets equal; null threshold == 20.0 default.");
        File.AppendAllText(EvidencePath, sb.ToString());

        // Prove the rollback left nothing behind.
        await using var check = CreateContext([], cs);
        Assert.Equal(0, await check.Users.IgnoreQueryFilters()
            .CountAsync(u => u.Email.EndsWith("@tx.test")));
    }

    private static User NewUser(Guid id, string first, string last, string email, UserRole role)
    {
        var user = new User
        {
            Id = id, FirstName = first, LastName = last, Email = email, Role = role,
        };
        typeof(User).GetProperty("PasswordHash")!.SetValue(user, "tx");
        return user;
    }
}

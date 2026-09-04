using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Services.Assignment;
using Xunit;

namespace EduCore.Tests;

public class ElementaryAssignmentSyncTests
{
    private static readonly Guid SchoolId = Guid.NewGuid();
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid SpecialistId = Guid.NewGuid();
    private static readonly Subject S1 = new() { Id = Guid.NewGuid(), Name = "Maths", SchoolId = SchoolId };
    private static readonly Subject S2 = new() { Id = Guid.NewGuid(), Name = "Science", SchoolId = SchoolId };

    private sealed class FakeSchoolRepository(SchoolType type) : ISchoolRepository
    {
        public Task<School?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<School?>(new School { Id = SchoolId, Name = "S", Type = type });
        public Task<School?> GetWithLocationAsync(Guid schoolId, CancellationToken ct) => Task.FromResult<School?>(null);
        public Task<IEnumerable<School>> GetAllOrderedAsync(bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<IEnumerable<School>> GetByCityAsync(Guid cityId, bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<IEnumerable<School>> GetByStateAsync(Guid stateId, bool activeOnly, CancellationToken ct) =>
            Task.FromResult<IEnumerable<School>>([]);
        public Task<bool> NameExistsAsync(string name, Guid? excludeSchoolId, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(School entity) => Task.CompletedTask;
        public void Update(School entity) { }
        public void Remove(School entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeCurriculum(IReadOnlyList<Subject> subjects) : IGradeCurriculumRepository
    {
        public Task<IReadOnlyList<Subject>> GetSubjectsForGradeAsync(int grade, CancellationToken ct) =>
            Task.FromResult(subjects);
        public Task<IReadOnlyList<GradeCurriculum>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<GradeCurriculum>>([]);
        public Task<GradeCurriculum?> GetAsync(int grade, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<GradeCurriculum?>(null);
        public Task<GradeCurriculum?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<GradeCurriculum?>(null);
        public Task AddAsync(GradeCurriculum entity) => Task.CompletedTask;
        public void Update(GradeCurriculum entity) { }
        public void Remove(GradeCurriculum entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeClassRepository : IClassRepository
    {
        public Task<Class?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Class?>(null);
        public Task<IEnumerable<EduCore.Dtos.Class.ClassListDto>> GetAllAsync(Guid? academicYearId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.Class.ClassListDto>>([]);
        public Task<EduCore.Dtos.Class.ClassListDto?> GetDtoAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<EduCore.Dtos.Class.ClassListDto?>(null);
        public Task<IEnumerable<EduCore.Dtos.Class.ClassListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.Class.ClassListDto>>([]);
        public Task<bool> NameExistsInYearAsync(Guid academicYearId, string name, Guid? excludeClassId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<IEnumerable<Class>> GetAllWithHomeroomTeacherAsync(CancellationToken ct) =>
            Task.FromResult<IEnumerable<Class>>([]);
        public Task AddAsync(Class entity) => Task.CompletedTask;
        public void Update(Class entity) { }
        public void Remove(Class entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeAssignments(IReadOnlyList<ClassSubjectTeacher> active) : IClassSubjectTeacherRepository
    {
        public readonly List<ClassSubjectTeacher> Added = [];
        public readonly List<ClassSubjectTeacher> Updated = [];

        public Task<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>> GetByTeacherAsync(
            Guid teacherId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>>([]);
        public Task<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>> GetByClassAsync(
            Guid classId, bool includeEnded, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto>>([]);
        public Task<ClassSubjectTeacher?> GetActiveAsync(Guid classId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(active.FirstOrDefault(a => a.SubjectId == subjectId));
        public Task<IReadOnlyList<ClassSubjectTeacher>> GetActiveForClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult(active);
        public Task<bool> TeachesClassAsync(Guid teacherId, Guid classId, CancellationToken ct) => Task.FromResult(false);
        public Task<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto?> GetDtoAsync(Guid assignmentId, CancellationToken ct) =>
            Task.FromResult<EduCore.Dtos.ClassSubjectTeacher.ClassSubjectTeacherListDto?>(null);
        public Task<ClassSubjectTeacher?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<ClassSubjectTeacher?>(null);
        public Task AddAsync(ClassSubjectTeacher entity) { Added.Add(entity); return Task.CompletedTask; }
        public void Update(ClassSubjectTeacher entity) => Updated.Add(entity);
        public void Remove(ClassSubjectTeacher entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeTeacherSubjects : ITeacherSubjectRepository
    {
        public Task<bool> ExistsAsync(Guid teacherId, Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task<IEnumerable<EduCore.Dtos.TeacherSubject.TeacherSubjectListDto>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<EduCore.Dtos.TeacherSubject.TeacherSubjectListDto>>([]);
        public Task<TeacherSubject?> GetAsync(Guid teacherId, Guid subjectId, CancellationToken ct) => Task.FromResult<TeacherSubject?>(null);
        public Task<IReadOnlyList<Guid>> GetSubjectIdsForTeacherAsync(Guid teacherId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> IsInUseAsync(Guid teacherId, Guid subjectId, CancellationToken ct) => Task.FromResult(false);
        public Task<TeacherSubject?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<TeacherSubject?>(null);
        public Task AddAsync(TeacherSubject entity) => Task.CompletedTask;
        public void Update(TeacherSubject entity) { }
        public void Remove(TeacherSubject entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private static ElementaryAssignmentSync Sync(SchoolType type, IReadOnlyList<Subject> subjects, FakeAssignments assignments) =>
        new(new FakeSchoolRepository(type), new FakeCurriculum(subjects), new FakeClassRepository(),
            assignments, new FakeTeacherSubjects());

    [Fact]
    public async Task Elementary_homeroom_teacher_is_assigned_every_grade_curriculum_subject()
    {
        var assignments = new FakeAssignments([]);
        var sync = Sync(SchoolType.Elementary, [S1, S2], assignments);

        await sync.SyncHomeroomChangeAsync(SchoolId, ClassId, grade: 3, oldTeacherId: null, newTeacherId: TeacherId, CancellationToken.None);

        Assert.Equal(2, assignments.Added.Count);
        Assert.All(assignments.Added, a => Assert.Equal(TeacherId, a.TeacherId));
        Assert.Contains(assignments.Added, a => a.SubjectId == S1.Id);
        Assert.Contains(assignments.Added, a => a.SubjectId == S2.Id);
    }

    [Fact]
    public async Task Non_elementary_school_is_a_no_op()
    {
        var assignments = new FakeAssignments([]);
        var sync = Sync(SchoolType.HighSchool, [S1, S2], assignments);

        await sync.SyncHomeroomChangeAsync(SchoolId, ClassId, grade: 10, oldTeacherId: null, newTeacherId: TeacherId, CancellationToken.None);

        Assert.Empty(assignments.Added);
    }

    [Fact]
    public async Task A_specialist_already_teaching_a_subject_is_left_in_place()
    {
        // A specialist teaches S1; the new homeroom teacher should only pick up S2.
        var specialist = new ClassSubjectTeacher { Id = Guid.NewGuid(), ClassId = ClassId, SubjectId = S1.Id, TeacherId = SpecialistId, SchoolId = SchoolId };
        var assignments = new FakeAssignments([specialist]);
        var sync = Sync(SchoolType.Elementary, [S1, S2], assignments);

        await sync.SyncHomeroomChangeAsync(SchoolId, ClassId, grade: 3, oldTeacherId: null, newTeacherId: TeacherId, CancellationToken.None);

        Assert.Single(assignments.Added);
        Assert.Equal(S2.Id, assignments.Added[0].SubjectId);
        Assert.DoesNotContain(assignments.Updated, a => a.SubjectId == S1.Id); // specialist not ended
    }
}

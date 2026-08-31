using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;

namespace EduCore.Services.Assignment;

public class ElementaryAssignmentSync(
    ISchoolRepository schools,
    ISubjectRepository subjects,
    IClassRepository classes,
    IClassSubjectTeacherRepository assignments,
    ITeacherSubjectRepository teacherSubjects) : IElementaryAssignmentSync
{
    public async Task SyncHomeroomChangeAsync(
        Guid schoolId, Guid classId, Guid? oldTeacherId, Guid? newTeacherId, CancellationToken ct)
    {
        if (oldTeacherId == newTeacherId || !await IsElementaryAsync(schoolId, ct))
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allSubjects = await subjects.GetAllOrderedAsync(ct);

        var activeBySubject = (await assignments.GetActiveForClassAsync(classId, ct))
            .ToDictionary(a => a.SubjectId);

        if (newTeacherId is null)
        {
            foreach (var subject in allSubjects)
            {
                if (activeBySubject.TryGetValue(subject.Id, out var active)
                    && active.TeacherId == oldTeacherId)
                {
                    active.EndedOn = today;
                    assignments.Update(active);
                }
            }

            return;
        }

        var qualified = (await teacherSubjects.GetSubjectIdsForTeacherAsync(newTeacherId.Value, ct))
            .ToHashSet();

        foreach (var subject in allSubjects)
        {
            if (!qualified.Contains(subject.Id))
            {
                qualified.Add(subject.Id);

                await teacherSubjects.AddAsync(new TeacherSubject
                {
                    TeacherId = newTeacherId.Value,
                    SubjectId = subject.Id,
                    SchoolId = schoolId
                });
            }

            activeBySubject.TryGetValue(subject.Id, out var activeAssignment);

            if (activeAssignment is not null && activeAssignment.TeacherId != oldTeacherId)
                continue;

            if (activeAssignment is not null)
            {
                activeAssignment.EndedOn = today;
                assignments.Update(activeAssignment);
            }

            await assignments.AddAsync(new ClassSubjectTeacher
            {
                ClassId = classId,
                SubjectId = subject.Id,
                TeacherId = newTeacherId.Value,
                SchoolId = schoolId
            });
        }
    }

    public async Task SyncNewSubjectAsync(Guid schoolId, Guid subjectId, CancellationToken ct)
    {
        if (!await IsElementaryAsync(schoolId, ct))
            return;

        var qualified = (await teacherSubjects.GetTeacherIdsForSubjectAsync(subjectId, ct))
            .ToHashSet();

        foreach (var classEntity in await classes.GetAllWithHomeroomTeacherAsync(ct))
        {
            var teacherId = classEntity.HomeroomTeacherId!.Value;

            if (!qualified.Contains(teacherId))
            {
                qualified.Add(teacherId);

                await teacherSubjects.AddAsync(new TeacherSubject
                {
                    TeacherId = teacherId,
                    SubjectId = subjectId,
                    SchoolId = schoolId
                });
            }

            await assignments.AddAsync(new ClassSubjectTeacher
            {
                ClassId = classEntity.Id,
                SubjectId = subjectId,
                TeacherId = teacherId,
                SchoolId = schoolId
            });
        }
    }

    private async Task<bool> IsElementaryAsync(Guid schoolId, CancellationToken ct) =>
        (await schools.GetByIdAsync(schoolId, ct))?.Type == SchoolType.Elementary;
}

using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;

namespace EduCore.Services.Assignment;

// No-ops for any school that isn't Elementary. For an elementary school, the homeroom
// teacher IS the subject teacher -- assigning homeroom grants every subject the school has,
// and changing it moves those subjects to the new teacher.
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

        if (newTeacherId is null)
        {
            // Homeroom cleared: end what the outgoing teacher held, and leave any specialist
            // override (a subject taught by someone else) exactly as it was.
            foreach (var subject in allSubjects)
            {
                var active = await assignments.GetActiveAsync(classId, subject.Id, ct);

                if (active is not null && active.TeacherId == oldTeacherId)
                {
                    active.EndedOn = today;
                    assignments.Update(active);
                }
            }

            return;
        }

        foreach (var subject in allSubjects)
        {
            if (!await teacherSubjects.ExistsAsync(newTeacherId.Value, subject.Id, ct))
                await teacherSubjects.AddAsync(new TeacherSubject
                {
                    TeacherId = newTeacherId.Value,
                    SubjectId = subject.Id,
                    SchoolId = schoolId
                });

            var activeAssignment = await assignments.GetActiveAsync(classId, subject.Id, ct);

            // Only take over a subject that had no teacher, or belonged to the outgoing
            // homeroom teacher. A specialist override -- taught by someone else -- is untouched.
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

        // A brand-new subject can have no existing assignment for anyone, so every class with
        // a homeroom teacher gets one created outright.
        foreach (var classEntity in await classes.GetAllWithHomeroomTeacherAsync(ct))
        {
            var teacherId = classEntity.HomeroomTeacherId!.Value;

            if (!await teacherSubjects.ExistsAsync(teacherId, subjectId, ct))
                await teacherSubjects.AddAsync(new TeacherSubject
                {
                    TeacherId = teacherId,
                    SubjectId = subjectId,
                    SchoolId = schoolId
                });

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

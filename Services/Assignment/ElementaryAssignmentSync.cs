using EduCore.Enums;
using EduCore.Models;
using EduCore.Repositories.Interfaces;

namespace EduCore.Services.Assignment;

// No-ops for any school that isn't Elementary. For an elementary school, the homeroom
// teacher IS the subject teacher -- assigning homeroom grants every subject the school has,
// and changing it moves those subjects to the new teacher.
//
// Both methods load what they need up front rather than asking per subject or per class.
// The loops below run once per subject a school has, and every question inside them was
// previously its own round trip -- so the cost of assigning a homeroom teacher grew with the
// size of the curriculum. Reading the sets once makes it two queries regardless.
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

        // Tracked entities, keyed by subject: ending one below is a plain property set.
        // A class has at most one active assignment per subject -- the filtered unique index
        // on (ClassId, SubjectId) where EndedOn is null says so -- which is what makes this
        // safe to key by SubjectId.
        var activeBySubject = (await assignments.GetActiveForClassAsync(classId, ct))
            .ToDictionary(a => a.SubjectId);

        if (newTeacherId is null)
        {
            // Homeroom cleared: end what the outgoing teacher held, and leave any specialist
            // override (a subject taught by someone else) exactly as it was.
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

        // What the incoming teacher is already qualified for. Added to as we go, so a subject
        // granted in this loop is not granted a second time -- the rows are not saved until
        // the caller's trailing save, so the database cannot answer that question for us.
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

        // Which teachers already hold this subject. Tracking it across the loop matters for
        // correctness, not just for speed: one teacher can be homeroom for two classes, and
        // granting them the same subject twice in a single save violates the unique key on
        // (TeacherId, SubjectId, SchoolId). Asking the database per class could not see the
        // grant made one iteration earlier, because none of them are saved yet.
        var qualified = (await teacherSubjects.GetTeacherIdsForSubjectAsync(subjectId, ct))
            .ToHashSet();

        // A brand-new subject can have no existing assignment for anyone, so every class with
        // a homeroom teacher gets one created outright.
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

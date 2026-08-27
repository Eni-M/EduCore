using AutoMapper;
using EduCore.Dtos.DiaryEntry;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Services;

public class DiaryEntryService(
    IDiaryEntryRepository diaryEntries,
    IAttendanceRepository attendances,
    IEnrollmentRepository enrollments,
    IClassRepository classes,
    ISubjectRepository subjects,
    ITeacherSubjectRepository teacherSubjects,
    IClassSubjectTeacherRepository assignments,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IDiaryEntryService
{
    public async Task<IEnumerable<DiaryEntryListDto>> GetByClassAsync(
        Guid classId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct)
    {
        await access.EnsureCanViewClassAsync(classId, ct);
        return await diaryEntries.GetByClassAsync(classId, from, to, ct);
    }

    public async Task<IEnumerable<DiaryEntryListDto>> GetMineAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct) =>
        await diaryEntries.GetByTeacherAsync(currentUser.RequireTeacherId(), from, to, ct);

    public async Task<DiaryEntryDetailDto> GetDetailAsync(Guid diaryEntryId, CancellationToken ct)
    {
        var entry = await GetOwnedAsync(diaryEntryId, ct);

        await access.EnsureCanViewClassAsync(entry.ClassId, ct);

        var detail = await diaryEntries.GetDetailAsync(diaryEntryId, ct)
                     ?? throw new NotFoundException("Diary entry", diaryEntryId);

        if (!currentUser.IsStudent)
            return detail;

        // The register lists every classmate's absences, comments and behavior marks. A
        // student may see the lesson, but only their own row of it.
        var mine = await enrollments.GetActiveForStudentAsync(currentUser.RequireStudentId(), ct);

        return detail with
        {
            Attendances = detail.Attendances.Where(a => a.EnrollmentId == mine?.Id).ToList()
        };
    }

    public async Task<DiaryEntryDetailDto> CreateAsync(DiaryEntryCreateDto dto, CancellationToken ct)
    {
        var @class = await classes.GetByIdAsync(dto.ClassId, ct)
                     ?? throw new NotFoundException("Class", dto.ClassId);

        _ = await subjects.GetByIdAsync(dto.SubjectId, ct) ?? throw new NotFoundException("Subject", dto.SubjectId);

        // A teacher always logs their own lesson, so an omitted id is theirs. An
        // administrator logging on someone's behalf has to say whose it was.
        var teacherId = dto.TeacherId
                        ?? (currentUser.IsTeacher
                            ? currentUser.RequireTeacherId()
                            : throw new BusinessException("Name the teacher who taught this lesson."));

        if (currentUser.IsTeacher && teacherId != currentUser.TeacherId)
            throw new ForbiddenException("You can only log lessons that you taught yourself.");

        // Without this a teacher could open a register for any class in the school and then
        // mark attendance and behavior for students they have nothing to do with.
        await access.EnsureCanTeachClassAsync(dto.ClassId, ct);

        if (!await teacherSubjects.ExistsAsync(teacherId, dto.SubjectId, ct))
            throw new BusinessException("This teacher is not qualified to teach that subject.");

        var everAssigned = (await assignments.GetByClassAsync(dto.ClassId, true, ct))
            .Any(a => a.SubjectId == dto.SubjectId);

        if (!everAssigned)
            throw new BusinessException("That subject is not taught in this class.");

        if (await diaryEntries.SlotTakenAsync(dto.ClassId, dto.Date, dto.Period, ct))
            throw new ConflictException("A lesson is already recorded for this class in that period.");

        var entry = mapper.Map<DiaryEntry>(dto);
        // Set explicitly, never left to the mapper: DiaryEntry.TeacherId is a non-nullable
        // Guid and the DTO's is now nullable, so an omitted id would map to Guid.Empty and
        // be written as a lesson taught by nobody.
        entry.TeacherId = teacherId;
        entry.SchoolId = currentUser.RequireSchoolId();

        // After AddAsync the entry has its Id, which the attendance rows below need --
        // Attendance has no navigation property back to DiaryEntry for EF to fix up.
        await diaryEntries.AddAsync(entry);

        foreach (var enrollment in await enrollments.GetActiveByClassAsync(dto.ClassId, ct))
        {
            // Everyone starts present; the teacher then marks the exceptions.
            await attendances.AddAsync(new Attendance
            {
                DiaryEntryId = entry.Id,
                EnrollmentId = enrollment.Id,
                ClassId = @class.Id,
                SchoolId = entry.SchoolId
            });
        }

        await diaryEntries.SaveChangesAsync(ct);

        return await diaryEntries.GetDetailAsync(entry.Id, ct)
               ?? throw new NotFoundException("Diary entry", entry.Id);
    }

    public async Task<DiaryEntryDetailDto> UpdateAsync(
        Guid diaryEntryId,
        DiaryEntryEditDto dto,
        CancellationToken ct)
    {
        var entry = await GetOwnedAsync(diaryEntryId, ct);

        await access.EnsureCanTeachClassAsync(entry.ClassId, ct);

        if (currentUser.IsTeacher && entry.TeacherId != currentUser.TeacherId)
            throw new ForbiddenException("You can only edit lessons you logged yourself.");

        mapper.Map(dto, entry);

        diaryEntries.Update(entry);
        await diaryEntries.SaveChangesAsync(ct);

        return await diaryEntries.GetDetailAsync(diaryEntryId, ct)
               ?? throw new NotFoundException("Diary entry", diaryEntryId);
    }

    private async Task<DiaryEntry> GetOwnedAsync(Guid diaryEntryId, CancellationToken ct) =>
        await diaryEntries.GetByIdAsync(diaryEntryId, ct)
        ?? throw new NotFoundException("Diary entry", diaryEntryId);
}

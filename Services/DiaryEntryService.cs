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
    IAcademicYearRepository academicYears,
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

        var teacherId = dto.TeacherId
                        ?? (currentUser.IsTeacher
                            ? currentUser.RequireTeacherId()
                            : throw new BusinessException("Name the teacher who taught this lesson."));

        if (currentUser.IsTeacher && teacherId != currentUser.TeacherId)
            throw new ForbiddenException("You can only log lessons that you taught yourself.");

        await access.EnsureCanTeachClassAsync(dto.ClassId, ct);

        if (!await teacherSubjects.ExistsAsync(teacherId, dto.SubjectId, ct))
            throw new BusinessException("This teacher is not qualified to teach that subject.");

        var everAssigned = (await assignments.GetByClassAsync(dto.ClassId, true, ct))
            .Any(a => a.SubjectId == dto.SubjectId);

        if (!everAssigned)
            throw new BusinessException("That subject is not taught in this class.");

        if (dto.Date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BusinessException("A lesson cannot be recorded before it has been taught.");

        var year = await academicYears.GetByIdAsync(@class.AcademicYearId, ct)
                   ?? throw new NotFoundException("Academic year", @class.AcademicYearId);

        if (dto.Date < year.StartDate || dto.Date > year.EndDate)
            throw new BusinessException(
                $"{year.Name} runs from {year.StartDate:yyyy-MM-dd} to {year.EndDate:yyyy-MM-dd}. " +
                "A lesson dated outside its own academic year would not show up in any report " +
                "for that year.");

        if (await diaryEntries.SlotTakenAsync(dto.ClassId, dto.Date, dto.Period, ct))
            throw new ConflictException("A lesson is already recorded for this class in that period.");

        var entry = mapper.Map<DiaryEntry>(dto);
        entry.TeacherId = teacherId;
        entry.SchoolId = currentUser.RequireSchoolId();

        await diaryEntries.AddAsync(entry);

        foreach (var enrollment in await enrollments.GetActiveByClassAsync(dto.ClassId, ct))
        {
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

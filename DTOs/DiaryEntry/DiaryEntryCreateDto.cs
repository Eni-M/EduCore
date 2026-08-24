namespace EduCore.Dtos.DiaryEntry;

// Creating a diary entry is expected to also generate one Attendance row per currently
// enrolled student in the class (defaulted to present), for the teacher to then mark up --
// that's a service-layer concern, not something this DTO carries. TeacherId may be a
// substitute, not necessarily the officially assigned ClassSubjectTeacher -- only same-school
// TeacherSubject qualification is required, checked server-side.
public record DiaryEntryCreateDto(
    Guid ClassId,
    Guid SubjectId,
    Guid TeacherId,
    DateOnly Date,
    int Period,
    string Topic,
    string? Notes);

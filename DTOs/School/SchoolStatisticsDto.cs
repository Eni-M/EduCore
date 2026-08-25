using EduCore.Enums;

namespace EduCore.Dtos.School;

public record SchoolStatisticsDto(
    Guid SchoolId,
    string SchoolName,
    SchoolType Type,
    int AdminCount,
    int TeacherCount,
    int StudentCount,
    int ActiveStudentCount,
    int ClassCount,
    int SubjectCount);
namespace EduCore.Dtos.Statistics.Individual;

public record TeacherWorkloadDto(
    Guid TeacherId,
    string TeacherName,
    Guid SchoolId,
    string SchoolName,
    int Classes,
    int Subjects,
    int Students,
    int LessonsLogged);

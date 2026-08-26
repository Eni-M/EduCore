namespace EduCore.Dtos.Statistics.Individual;

public record TeacherWorkloadDto(
    Guid TeacherId,
    string TeacherName,
    int Classes,
    int Subjects,
    int Students,
    int LessonsLogged);

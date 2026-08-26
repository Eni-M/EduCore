namespace EduCore.Dtos.Statistics.Aggregate;

public record TeacherStatisticsDto(
    int Teachers,
    Measure StudentsPerTeacher,
    Measure ClassesPerTeacher);

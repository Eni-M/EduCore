namespace EduCore.Dtos.Statistics.Individual;

// SchoolId/SchoolName are always populated, not just for a SuperAdmin: a cross-school
// listing is one flat table, and without them rows from different schools are
// indistinguishable -- two teachers with the same name, or the same class name reused.
public record TeacherWorkloadDto(
    Guid TeacherId,
    string TeacherName,
    Guid SchoolId,
    string SchoolName,
    int Classes,
    int Subjects,
    int Students,
    int LessonsLogged);

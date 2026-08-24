using EduCore.Data;

namespace EduCore.Models;

// One row per lesson actually taught (the "digital diary" entry). Stores ClassId/SubjectId/
// TeacherId directly rather than a ClassSubjectTeacherId, so that reassigning who teaches a
// class-subject pairing later can never retroactively change who taught a past lesson, and
// so a substitute teacher can log a lesson without being the officially assigned teacher.
public class DiaryEntry : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SchoolId { get; set; }
    public DateOnly Date { get; set; }
    public int Period { get; set; }
    public string Topic { get; set; } = null!;
    public string? Notes { get; set; }
}

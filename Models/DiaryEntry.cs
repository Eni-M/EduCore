using EduCore.Data;

namespace EduCore.Models;

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

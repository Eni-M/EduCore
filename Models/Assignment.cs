using EduCore.Data;

namespace EduCore.Models;

public class Assignment : ISchoolScoped
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public Guid ClassSubjectTeacherId { get; set; }
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTimeOffset DueDate { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }

    public string? StorageKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}


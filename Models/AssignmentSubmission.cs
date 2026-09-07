using EduCore.Data;

namespace EduCore.Models;

public class AssignmentSubmission : ISchoolScoped
{
    public Guid Id { get; set; }

    public Guid AssignmentId { get; set; }

    public Guid StudentId { get; set; }

    public Guid SchoolId { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }

    public string? StorageKey { get; set; }

    public DateTime SubmittedAt { get; set; }

    public decimal? Score { get; set; }

    public string? Feedback { get; set; }
}


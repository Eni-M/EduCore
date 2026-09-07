namespace EduCore.Dtos.Assignment;

// Id is null and Status is "Missing" for an enrolled student who hasn't submitted --
// GetForAssignmentAsync returns one row per enrolled student, not per submission row.
public record AssignmentSubmissionDto(
    Guid? Id,
    Guid AssignmentId,
    Guid StudentId,
    string StudentName,
    string Status,
    DateTime? SubmittedAt,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    decimal? Score,
    string? Feedback);
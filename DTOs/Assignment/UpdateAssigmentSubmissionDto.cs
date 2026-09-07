namespace EduCore.Dtos.Assignment;

public record UpdateAssignmentSubmissionDto(
    decimal? Score,
    string? Feedback);
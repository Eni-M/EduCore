namespace EduCore.Dtos.Assignment;

public record CreateSubmissionRequest(
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageKey);
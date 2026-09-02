using EduCore.Enums;

namespace EduCore.Dtos.StudentDocument;

// Metadata only. The bytes are fetched separately from the /content endpoint so a listing stays
// cheap and never streams a file it does not need to.
public record StudentDocumentListDto(
    Guid Id,
    Guid StudentId,
    string Title,
    DocumentCategory Category,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt);

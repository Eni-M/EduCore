using EduCore.Dtos.StudentDocument;
using EduCore.Enums;

namespace EduCore.Services.Interfaces;

public interface IStudentDocumentService
{
    Task<IEnumerable<StudentDocumentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct);

    // Primitives, not IFormFile, so the service stays framework-agnostic and testable; the
    // endpoint pulls these off the uploaded file.
    Task<StudentDocumentListDto> UploadAsync(
        Guid studentId,
        string title,
        DocumentCategory category,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken ct);

    Task<(Stream Content, string ContentType, string FileName)> OpenAsync(
        Guid studentId, Guid documentId, CancellationToken ct);

    Task DeleteAsync(Guid studentId, Guid documentId, CancellationToken ct);
}

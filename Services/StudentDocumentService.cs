using AutoMapper;
using EduCore.Dtos.StudentDocument;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services.Interfaces;
using EduCore.Services.Storage;
using Microsoft.Extensions.Options;

namespace EduCore.Services;

public class StudentDocumentService(
    IStudentDocumentRepository documents,
    IStudentRepository students,
    IFileStorage storage,
    IOptions<StorageOptions> storageOptions,
    IAccessGuard access,
    ICurrentUser currentUser,
    IMapper mapper) : IStudentDocumentService
{
    private readonly StorageOptions _storage = storageOptions.Value;

    public async Task<IEnumerable<StudentDocumentListDto>> GetByStudentAsync(Guid studentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        return mapper.Map<IEnumerable<StudentDocumentListDto>>(await documents.GetByStudentAsync(studentId, ct));
    }

    public async Task<StudentDocumentListDto> UploadAsync(
        Guid studentId,
        string title,
        DocumentCategory category,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken ct)
    {
        // School-filtered read: a document cannot be attached to a student in another school.
        var student = await students.GetByIdAsync(studentId, ct)
                      ?? throw new NotFoundException("Student", studentId);

        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessException("A document needs a title.");

        if (sizeBytes <= 0)
            throw new BusinessException("The uploaded file is empty.");

        if (sizeBytes > _storage.MaxFileSizeBytes)
            throw new BusinessException(
                $"The file is larger than the {_storage.MaxFileSizeBytes / (1024 * 1024)} MB limit.");

        if (_storage.AllowedContentTypes.Length > 0
            && !_storage.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException($"Files of type '{contentType}' are not allowed.");

        var key = await storage.SaveAsync(
            content, $"{student.SchoolId}/{studentId}", Path.GetExtension(fileName), ct);

        var document = new StudentDocument
        {
            StudentId = studentId,
            SchoolId = student.SchoolId,
            Title = title.Trim(),
            Category = category,
            FileName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = sizeBytes,
            StorageKey = key,
            UploadedByUserId = currentUser.UserId,
            UploadedAt = DateTime.UtcNow
        };

        await documents.AddAsync(document);
        await documents.SaveChangesAsync(ct);

        return mapper.Map<StudentDocumentListDto>(document);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenAsync(
        Guid studentId, Guid documentId, CancellationToken ct)
    {
        await access.EnsureCanViewStudentAsync(studentId, ct);

        var document = await OwnedAsync(studentId, documentId, ct);
        var stream = await storage.OpenReadAsync(document.StorageKey, ct);

        return (stream, document.ContentType, document.FileName);
    }

    public async Task DeleteAsync(Guid studentId, Guid documentId, CancellationToken ct)
    {
        _ = await students.GetByIdAsync(studentId, ct) ?? throw new NotFoundException("Student", studentId);

        var document = await OwnedAsync(studentId, documentId, ct);

        // Row first, then the bytes: a failed row delete leaves the file, which is recoverable;
        // a deleted file with a surviving row would be a listing that 404s on download.
        documents.Remove(document);
        await documents.SaveChangesAsync(ct);
        await storage.DeleteAsync(document.StorageKey, ct);
    }

    private async Task<StudentDocument> OwnedAsync(Guid studentId, Guid documentId, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(documentId, ct)
                       ?? throw new NotFoundException("Document", documentId);

        if (document.StudentId != studentId)
            throw new NotFoundException("Document", documentId);

        return document;
    }
}

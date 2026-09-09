using EduCore.Data;
using EduCore.Enums;
using EduCore.Services.Interfaces;

namespace EduCore.Models;

// Metadata for a file kept for a student. The bytes live in IFileStorage under StorageKey; only
// the opaque key is stored here, never a filesystem path, so the storage backend can change
// (local disk today, cloud later) without touching this row.
public class StudentDocument : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SchoolId { get; set; }
    public string Title { get; set; } = null!;
    public DocumentCategory Category { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = null!;
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
}

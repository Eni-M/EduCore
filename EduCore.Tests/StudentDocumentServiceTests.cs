using AutoMapper;
using EduCore.Enums;
using EduCore.Exceptions;
using EduCore.Mapping;
using EduCore.Models;
using EduCore.Repositories.Interfaces;
using EduCore.Security;
using EduCore.Services;
using EduCore.Services.Interfaces;
using EduCore.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EduCore.Tests;

public class StudentDocumentServiceTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid SchoolId = Guid.NewGuid();

    private sealed class FakeStudentRepository : IStudentRepository
    {
        public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<Student?>(id == StudentId ? new Student { Id = StudentId, SchoolId = SchoolId } : null);
        public Task<Student?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult<Student?>(null);
        public Task<Student?> GetWithGuardiansAsync(Guid studentId, CancellationToken ct) => Task.FromResult<Student?>(null);
        public Task<bool> PersonalNumberExistsAsync(string personalNumber, Guid? excludeStudentId, CancellationToken ct) =>
            Task.FromResult(false);
        public Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? search, bool includeGraduated, CancellationToken ct) =>
            Task.FromResult<(IReadOnlyList<Student>, int)>(([], 0));
        public Task<IEnumerable<Student>> GetByClassAsync(Guid classId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<Student>>([]);
        public Task AddAsync(Student entity) => Task.CompletedTask;
        public void Update(Student entity) { }
        public void Remove(Student entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeDocumentRepository : IStudentDocumentRepository
    {
        public StudentDocument? Added;
        public Task<IEnumerable<StudentDocument>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
            Task.FromResult<IEnumerable<StudentDocument>>([]);
        public Task<StudentDocument?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Added);
        public Task AddAsync(StudentDocument entity) { Added = entity; return Task.CompletedTask; }
        public void Update(StudentDocument entity) { }
        public void Remove(StudentDocument entity) { }
        public Task<bool> SaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public bool Saved;
        public Task<string> SaveAsync(Stream content, string relativeDirectory, string extension, CancellationToken ct)
        {
            Saved = true;
            return Task.FromResult($"{relativeDirectory}/stored{extension}");
        }
        public Task<Stream> OpenReadAsync(string key, CancellationToken ct) => Task.FromResult<Stream>(new MemoryStream());
        public Task DeleteAsync(string key, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAccessGuard : IAccessGuard
    {
        public Task EnsureCanViewClassAsync(Guid classId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanTeachClassAsync(Guid classId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanViewStudentAsync(Guid studentId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureCanViewStudentMedicalAsync(Guid studentId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.NewGuid();
        public UserRole? Role => UserRole.Directorate;
        public Guid? SchoolId => StudentDocumentServiceTests.SchoolId;
        public IReadOnlyCollection<Guid> SchoolIds => [StudentDocumentServiceTests.SchoolId];
        public Guid? TeacherId => null;
        public Guid? StudentId => null;
        public Guid? CityId => null;
        public Guid? StateId => null;
        public Guid? GuardianId => null;
        public bool IsStateAdmin => false;
        public bool IsRegionalAdmin => false;
        public bool IsDirectorate => true;
        public bool IsTeacher => false;
        public bool IsStudent => false;
        public bool IsGuardian => false;
    }

    private static IMapper Mapper() =>
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static (StudentDocumentService Service, FakeFileStorage Storage, FakeDocumentRepository Docs) Build()
    {
        var storage = new FakeFileStorage();
        var docs = new FakeDocumentRepository();
        var options = Options.Create(new StorageOptions
        {
            MaxFileSizeBytes = 100,
            AllowedContentTypes = ["application/pdf"]
        });

        var service = new StudentDocumentService(
            docs, new FakeStudentRepository(), storage, options, new FakeAccessGuard(), new FakeCurrentUser(), Mapper());

        return (service, storage, docs);
    }

    [Fact]
    public async Task Rejects_a_file_over_the_size_limit()
    {
        var (service, storage, _) = Build();

        await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(
            StudentId, "Report", DocumentCategory.Report, "big.pdf", "application/pdf",
            sizeBytes: 500, new MemoryStream(new byte[500]), CancellationToken.None));

        Assert.False(storage.Saved);
    }

    [Fact]
    public async Task Rejects_a_disallowed_content_type()
    {
        var (service, storage, _) = Build();

        await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(
            StudentId, "Virus", DocumentCategory.Other, "run.exe", "application/x-msdownload",
            sizeBytes: 10, new MemoryStream(new byte[10]), CancellationToken.None));

        Assert.False(storage.Saved);
    }

    [Fact]
    public async Task Stores_a_valid_file_and_records_its_metadata()
    {
        var (service, storage, docs) = Build();

        var result = await service.UploadAsync(
            StudentId, "  Birth certificate  ", DocumentCategory.Certificate, "cert.pdf", "application/pdf",
            sizeBytes: 42, new MemoryStream(new byte[42]), CancellationToken.None);

        Assert.True(storage.Saved);
        Assert.NotNull(docs.Added);
        Assert.Equal(SchoolId, docs.Added!.SchoolId);
        Assert.Equal("Birth certificate", docs.Added.Title);
        Assert.False(string.IsNullOrEmpty(docs.Added.StorageKey));
        Assert.Equal("Birth certificate", result.Title);
    }
}

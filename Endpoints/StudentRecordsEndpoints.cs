using EduCore.Dtos.Medical;
using EduCore.Dtos.SchoolingHistory;
using EduCore.Enums;
using EduCore.Security;
using EduCore.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Endpoints;

// Records that hang off a single student: their schooling history, their saved documents, and
// their medical record. Reads are open to anyone who may see the student (StudentReadable, then
// the service narrows medical); writes are the school's own Directorate.
public static class StudentRecordsEndpoints
{
    public static void MapStudentRecordsEndpoints(this IEndpointRouteBuilder api)
    {
        var students = api.MapGroup("/students").WithTags("Student records");

        // ---- Schooling history -------------------------------------------
        students.MapGet("/{studentId:guid}/schooling-history",
                async (Guid studentId, ISchoolingHistoryService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        students.MapPost("/{studentId:guid}/schooling-history",
                async (Guid studentId, SchoolingHistoryCreateDto dto, ISchoolingHistoryService service,
                        CancellationToken ct) =>
                {
                    var created = await service.CreateAsync(studentId, dto, ct);
                    return Results.Created($"/api/students/{studentId}/schooling-history/{created.Id}", created);
                })
            .RequireAuthorization(Policies.Directorate);

        students.MapPut("/{studentId:guid}/schooling-history/{historyId:guid}",
                async (Guid studentId, Guid historyId, SchoolingHistoryEditDto dto, ISchoolingHistoryService service,
                        CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(studentId, historyId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        students.MapDelete("/{studentId:guid}/schooling-history/{historyId:guid}",
                async (Guid studentId, Guid historyId, ISchoolingHistoryService service, CancellationToken ct) =>
                {
                    await service.DeleteAsync(studentId, historyId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Directorate);


        // ---- Academic history (cross-school) -----------------------------
        // StudentReadable, the same audience as documents: directorate, teacher, the student and
        // their guardian. The service re-checks with IAccessGuard, because the policy only says
        // what kind of user this is, not which students they may see.
        students.MapGet("/{studentId:guid}/academic-history",
                async (Guid studentId, IStudentAcademicHistoryService service, CancellationToken ct) =>
                    Results.Ok(await service.GetAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        // Recompiles this student's records from the live enrollment and mark tables. Idempotent,
        // so it is safe to retry; restricted to directorate because it writes.
        students.MapPost("/{studentId:guid}/academic-history/rebuild",
                async (Guid studentId, IStudentAcademicHistoryService service, CancellationToken ct) =>
                    Results.Ok(new { years = await service.RebuildAsync(studentId, ct) }))
            .RequireAuthorization(Policies.Directorate);
        // ---- Documents ---------------------------------------------------
        students.MapGet("/{studentId:guid}/documents",
                async (Guid studentId, IStudentDocumentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        // Multipart upload. DisableAntiforgery because this app guards state-changing requests
        // with its own double-submit CSRF cookie (CsrfProtectionMiddleware), not ASP.NET's
        // antiforgery, which would otherwise reject form posts here for a missing token.
        students.MapPost("/{studentId:guid}/documents",
                async (Guid studentId, IFormFile file, [FromForm] string title, [FromForm] DocumentCategory category,
                        IStudentDocumentService service, CancellationToken ct) =>
                {
                    await using var stream = file.OpenReadStream();
                    var created = await service.UploadAsync(
                        studentId, title, category, file.FileName, file.ContentType, file.Length, stream, ct);
                    return Results.Created($"/api/students/{studentId}/documents/{created.Id}", created);
                })
            .RequireAuthorization(Policies.Directorate)
            .DisableAntiforgery();

        students.MapGet("/{studentId:guid}/documents/{documentId:guid}/content",
                async (Guid studentId, Guid documentId, IStudentDocumentService service, CancellationToken ct) =>
                {
                    var (content, contentType, fileName) = await service.OpenAsync(studentId, documentId, ct);
                    return Results.File(content, contentType, fileName);
                })
            .RequireAuthorization(Policies.StudentReadable);

        students.MapDelete("/{studentId:guid}/documents/{documentId:guid}",
                async (Guid studentId, Guid documentId, IStudentDocumentService service, CancellationToken ct) =>
                {
                    await service.DeleteAsync(studentId, documentId, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Directorate);

        // ---- Medical -----------------------------------------------------
        // Read is StudentReadable at the door; the service's medical guard then narrows a teacher
        // down to the homeroom teacher alone. Write is Directorate.
        students.MapGet("/{studentId:guid}/medical",
                async (Guid studentId, IStudentMedicalRecordService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        students.MapPut("/{studentId:guid}/medical",
                async (Guid studentId, StudentMedicalRecordUpsertDto dto, IStudentMedicalRecordService service,
                        CancellationToken ct) =>
                    Results.Ok(await service.UpsertAsync(studentId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        // A guardian maintains their own child's record; the service verifies the link and notifies
        // the directorate to review the change.
        students.MapPut("/{studentId:guid}/medical/by-guardian",
                async (Guid studentId, StudentMedicalRecordUpsertDto dto, IStudentMedicalRecordService service,
                        CancellationToken ct) =>
                    Results.Ok(await service.UpsertByGuardianAsync(studentId, dto, ct)))
            .RequireAuthorization(Policies.Guardian);
    }
}

using EduCore.Dtos.Pagination;
using EduCore.Dtos.Student;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/students").WithTags("Students");

        group.MapGet("/", async (int? page, int? pageSize, string? search, bool? includeGraduated,
                    IStudentService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetPagedAsync(
                    new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, includeGraduated ?? false, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/me", async (IStudentService service, ICurrentUser currentUser, CancellationToken ct) =>
                TypedResults.Ok(await service.GetProfileAsync(currentUser.RequireStudentId(), ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{studentId:guid}", async (Guid studentId, IStudentService service, CancellationToken ct) =>
                TypedResults.Ok(await service.GetByIdAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapGet("/{studentId:guid}/profile",
                async (Guid studentId, IStudentService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.GetProfileAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapPost("/", async (StudentCreateDto dto, IStudentService service, CancellationToken ct) =>
            {
                var student = await service.CreateAsync(dto, ct);
                return TypedResults.Created($"/api/students/{student.Id}", student);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapPut("/{studentId:guid}",
                async (Guid studentId, StudentEditDto dto, IStudentService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.UpdateAsync(studentId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/{studentId:guid}/graduation-eligibility",
                async (Guid studentId, IStudentService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.GetGraduationEligibilityAsync(studentId, ct)))
            .RequireAuthorization(Policies.Directorate);

        group.MapPost("/{studentId:guid}/graduate",
                async (Guid studentId, StudentGraduateDto dto, IStudentService service, CancellationToken ct) =>
                {
                    await service.GraduateAsync(studentId, dto, ct);
                    return TypedResults.NoContent();
                })
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/{studentId:guid}/guardians",
                async (Guid studentId, IGuardianService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapPost("/{studentId:guid}/guardians/{guardianId:guid}",
                async (Guid studentId, Guid guardianId, IGuardianService service, CancellationToken ct) =>
                {
                    await service.LinkAsync(studentId, guardianId, ct);
                    return TypedResults.NoContent();
                })
            .RequireAuthorization(Policies.Directorate);

        group.MapDelete("/{studentId:guid}/guardians/{guardianId:guid}",
                async (Guid studentId, Guid guardianId, IGuardianService service, CancellationToken ct) =>
                {
                    await service.UnlinkAsync(studentId, guardianId, ct);
                    return TypedResults.NoContent();
                })
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/{studentId:guid}/enrollments",
                async (Guid studentId, IEnrollmentService service, CancellationToken ct) =>
                    TypedResults.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapGet("/{studentId:guid}/attendance",
                async (Guid studentId, DateOnly? from, DateOnly? to, IAttendanceService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(await service.GetByStudentAsync(studentId, from, to, ct)))
            .RequireAuthorization(Policies.StudentReadable);

        group.MapGet("/{studentId:guid}/marks",
                async (Guid studentId, Guid? subjectId, Guid? gradingPeriodId, IMarkService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(await service.GetByStudentAsync(studentId, subjectId, gradingPeriodId, ct)))
            .RequireAuthorization(Policies.StudentReadable);
    }
}

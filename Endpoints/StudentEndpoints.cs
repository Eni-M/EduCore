using EduCore.Dtos.Pagination;
using EduCore.Dtos.Parent;
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
                Results.Ok(await service.GetPagedAsync(
                    new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, includeGraduated ?? false, ct)))
            .RequireAuthorization(Policies.Admin);

        // So the front end does not have to keep track of its own student id.
        group.MapGet("/me", async (IStudentService service, ICurrentUser currentUser, CancellationToken ct) =>
                Results.Ok(await service.GetProfileAsync(currentUser.RequireStudentId(), ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{studentId:guid}", async (Guid studentId, IStudentService service, CancellationToken ct) =>
                Results.Ok(await service.GetByIdAsync(studentId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{studentId:guid}/profile",
                async (Guid studentId, IStudentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetProfileAsync(studentId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/", async (StudentCreateDto dto, IStudentService service, CancellationToken ct) =>
            {
                var student = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/students/{student.Id}", student);
            })
            .RequireAuthorization(Policies.Admin);

        group.MapPut("/{studentId:guid}",
                async (Guid studentId, StudentEditDto dto, IStudentService service, CancellationToken ct) =>
                    Results.Ok(await service.UpdateAsync(studentId, dto, ct)))
            .RequireAuthorization(Policies.Admin);

        // Read-only twin of the POST below: the same rule, asked rather than applied, so the
        // student page can say why the button is disabled instead of guessing at it.
        group.MapGet("/{studentId:guid}/graduation-eligibility",
                async (Guid studentId, IStudentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetGraduationEligibilityAsync(studentId, ct)))
            .RequireAuthorization(Policies.Admin);

        group.MapPost("/{studentId:guid}/graduate",
                async (Guid studentId, StudentGraduateDto dto, IStudentService service, CancellationToken ct) =>
                {
                    await service.GraduateAsync(studentId, dto, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Admin);

        group.MapGet("/{studentId:guid}/parents",
                async (Guid studentId, IParentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{studentId:guid}/enrollments",
                async (Guid studentId, IEnrollmentService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{studentId:guid}/attendance",
                async (Guid studentId, DateOnly? from, DateOnly? to, IAttendanceService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, from, to, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{studentId:guid}/marks",
                async (Guid studentId, Guid? subjectId, Guid? gradingPeriodId, IMarkService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetByStudentAsync(studentId, subjectId, gradingPeriodId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        var parents = api.MapGroup("/parents")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Parents");

        parents.MapPost("/", async (ParentCreateDto dto, IParentService service, CancellationToken ct) =>
            Results.Ok(await service.CreateAsync(dto, ct)));

        parents.MapPut("/{parentId:guid}",
            async (Guid parentId, ParentEditDto dto, IParentService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(parentId, dto, ct)));

        parents.MapDelete("/{parentId:guid}", async (Guid parentId, IParentService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(parentId, ct);
            return Results.NoContent();
        });
    }
}

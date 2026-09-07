using EduCore.Dtos.Assignment;
using EduCore.Security;
using EduCore.Services.Assignments;

namespace EduCore.Endpoints;

public static class AssignmentEndpoints
{
    public static void MapAssignmentEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/assignments")
            .WithTags("Assignments");

        group.MapGet("/class/{classId:guid}",
                async (
                    Guid classId,
                    IAssignmentService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetByClassAsync(classId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/teacher/{teacherId:guid}",
                async (
                    Guid teacherId,
                    IAssignmentService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetByTeacherAsync(teacherId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{assignmentId:guid}",
                async (
                    Guid assignmentId,
                    IAssignmentService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetDetailsAsync(assignmentId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/",
                async (
                    CreateAssignmentRequest dto,
                    IAssignmentService service,
                    CancellationToken ct) =>
                {
                    var assignment = await service.CreateAsync(dto, ct);

                    return Results.Created(
                        $"/api/assignments/{assignment.Id}",
                        assignment);
                })
            .RequireAuthorization(Policies.Teacher);
    }
}
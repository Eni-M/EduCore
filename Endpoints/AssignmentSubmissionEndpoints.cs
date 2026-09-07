using EduCore.Dtos.Assignment;
using EduCore.Security;
using EduCore.Services.Assignments;

namespace EduCore.Endpoints;

public static class AssignmentSubmissionEndpoints
{
    public static void MapAssignmentSubmissionEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/assignments/{assignmentId:guid}/submissions")
            .WithTags("Assignments");

        // Student-only in practice: RequireStudentId() inside the service throws for
        // anyone without a student identity, since there's no Student-only policy defined.
        group.MapPost("/",
                async (
                    Guid assignmentId,
                    CreateSubmissionRequest dto,
                    IAssignmentSubmissionService service,
                    CancellationToken ct) =>
                {
                    var submission = await service.SubmitAsync(assignmentId, dto, ct);
                    return Results.Ok(submission);
                })
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/",
                async (
                    Guid assignmentId,
                    IAssignmentSubmissionService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GetForAssignmentAsync(assignmentId, ct)))
            .RequireAuthorization(Policies.Teacher);

        group.MapPut("/{studentId:guid}",
                async (
                    Guid assignmentId,
                    Guid studentId,
                    UpdateAssignmentSubmissionDto dto,
                    IAssignmentSubmissionService service,
                    CancellationToken ct) =>
                    Results.Ok(await service.GradeAsync(assignmentId, studentId, dto, ct)))
            .RequireAuthorization(Policies.Teacher);
    }
}
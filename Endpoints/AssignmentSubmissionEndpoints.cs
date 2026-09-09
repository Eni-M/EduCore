using EduCore.Dtos.Assignment;
using EduCore.Security;
using EduCore.Services.Assignments;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class AssignmentSubmissionEndpoints
{
    public static void MapAssignmentSubmissionEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/assignments/{assignmentId:guid}/submissions")
            .WithTags("Assignments");

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
using EduCore.Dtos.Assignment;
using EduCore.Security;
using EduCore.Services.Assignments;
using EduCore.Services.Interfaces;

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
                    TypedResults.Ok(await service.GetByClassAsync(classId, ct)))
            // Guardians read their children's work too; EnsureCanViewClassAsync limits them to
            // classes a child of theirs is in.
            .RequireAuthorization(Policies.StudentReadable);

        group.MapGet("/teacher/{teacherId:guid}",
                async (
                    Guid teacherId,
                    IAssignmentService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(await service.GetByTeacherAsync(teacherId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{assignmentId:guid}",
                async (
                    Guid assignmentId,
                    IAssignmentService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(await service.GetDetailsAsync(assignmentId, ct)))
            // Guardians read their children's work too; EnsureCanViewClassAsync limits them to
            // classes a child of theirs is in.
            .RequireAuthorization(Policies.StudentReadable);

        group.MapPost("/",
                async (
                    CreateAssignmentRequest dto,
                    IAssignmentService service,
                    CancellationToken ct) =>
                {
                    var assignment = await service.CreateAsync(dto, ct);

                    return TypedResults.Created(
                        $"/api/assignments/{assignment.Id}",
                        assignment);
                })
            .RequireAuthorization(Policies.Teacher);

        group.MapPost("/{assignmentId:guid}/file",
         async (
             Guid assignmentId,
             Microsoft.AspNetCore.Http.IFormFile file,
             IAssignmentService service,
             CancellationToken ct) =>
         {
             var updated = await service.UploadAssignmentFileAsync(assignmentId, file, ct);
             return TypedResults.Ok(updated);
         })
     .RequireAuthorization(Policies.Teacher)
     .DisableAntiforgery();

        group.MapPost("/{assignmentId:guid}/submissions/upload",
                async (
                    Guid assignmentId,
                    Microsoft.AspNetCore.Http.IFormFile file,
                    IAssignmentService service,
                    CancellationToken ct) =>
                {
                    var submission = await service.SubmitAssignmentAsync(assignmentId, file, ct);
                    return TypedResults.Created($"/api/assignments/{assignmentId}/submissions/{submission.Id}", submission);
                })
            .RequireAuthorization(Policies.StudentReadable)
            .DisableAntiforgery();

        // Student: get my submission for the assignment
        group.MapGet("/{assignmentId:guid}/submissions/mine",
                async (
                    Guid assignmentId,
                    IAssignmentService service,
                    CancellationToken ct) =>
                    TypedResults.Ok(await service.GetMySubmissionAsync(assignmentId, ct)))
            .RequireAuthorization(Policies.StudentReadable);
    }
}
using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class AcademicYearEndpoints
{
    public static void MapAcademicYearEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/academic-years").WithTags("Academic years");

        group.MapGet("/", async (IAcademicYearService service, CancellationToken ct) =>
                Results.Ok(await service.GetAllAsync(ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapGet("/{academicYearId:guid}",
                async (Guid academicYearId, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByIdAsync(academicYearId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/", async (AcademicYearCreateDto dto, IAcademicYearService service, CancellationToken ct) =>
            {
                var year = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/academic-years/{year.Id}", year);
            })
            .RequireAuthorization(Policies.Admin);

        // Grading periods only exist inside a year, so they hang off its route.
        group.MapGet("/{academicYearId:guid}/grading-periods",
                async (Guid academicYearId, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.GetGradingPeriodsAsync(academicYearId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/grading-periods",
                async (GradingPeriodCreateDto dto, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.CreateGradingPeriodAsync(dto, ct)))
            .RequireAuthorization(Policies.Admin);
    }
}

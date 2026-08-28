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

        // AdminOrSuperAdmin, not AnySchoolUser: a SuperAdmin belongs to no school and so was
        // excluded from that policy, which made this 403 for them -- and the statistics
        // period picker swallowed the failure and showed an empty list of years.
        group.MapGet("/", async (Guid? schoolId, IAcademicYearService service, CancellationToken ct) =>
                Results.Ok(await service.GetAllAsync(schoolId, ct)))
            .RequireAuthorization(Policies.AdminOrSuperAdmin);

        group.MapGet("/{academicYearId:guid}",
                async (Guid academicYearId, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByIdAsync(academicYearId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        // Also creates the year's two grading periods -- an academic year always has exactly
        // two, so there is no separate "add a period" call.
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

        // The only edit a grading period ever gets: moving its own dates. There is no add or
        // remove, since a year always has exactly two.
        group.MapPut("/grading-periods/{gradingPeriodId:guid}",
                async (Guid gradingPeriodId, GradingPeriodEditDto dto, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.EditGradingPeriodAsync(gradingPeriodId, dto, ct)))
            .RequireAuthorization(Policies.Admin);
    }
}

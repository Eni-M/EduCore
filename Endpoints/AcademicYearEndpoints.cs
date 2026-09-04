using EduCore.Dtos.AcademicYear;
using EduCore.Dtos.GradingPeriod;
using EduCore.Dtos.Pagination;
using EduCore.Helpers;
using EduCore.Security;
using EduCore.Services.Interfaces;

namespace EduCore.Endpoints;

public static class AcademicYearEndpoints
{
    public static void MapAcademicYearEndpoints(this IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/academic-years").WithTags("Academic years");

        // Statistics' PeriodPicker (State/Regional admins, not just Directorate) reads this list.
        group.MapGet("/", async (Guid? schoolId, int? page, int? pageSize, string? search,
                IAcademicYearService service, CancellationToken ct) =>
                page is null && pageSize is null && string.IsNullOrWhiteSpace(search)
                    ? Results.Ok(await service.GetAllAsync(schoolId, ct))
                    : Results.Ok(await service.GetPagedAsync(
                        schoolId, new PaginationQueryDto(page ?? 1, pageSize ?? 20), search, ct)))
            .RequireAuthorization(Policies.StatisticsViewer);

        group.MapGet("/{academicYearId:guid}",
                async (Guid academicYearId, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.GetByIdAsync(academicYearId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPost("/", async (AcademicYearCreateDto dto, IAcademicYearService service, CancellationToken ct) =>
            {
                var year = await service.CreateAsync(dto, ct);
                return Results.Created($"/api/academic-years/{year.Id}", year);
            })
            .RequireAuthorization(Policies.Directorate);

        group.MapGet("/{academicYearId:guid}/grading-periods",
                async (Guid academicYearId, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.GetGradingPeriodsAsync(academicYearId, ct)))
            .RequireAuthorization(Policies.AnySchoolUser);

        group.MapPut("/grading-periods/{gradingPeriodId:guid}",
                async (Guid gradingPeriodId, GradingPeriodEditDto dto, IAcademicYearService service, CancellationToken ct) =>
                    Results.Ok(await service.EditGradingPeriodAsync(gradingPeriodId, dto, ct)))
            .RequireAuthorization(Policies.Directorate);
    }
}

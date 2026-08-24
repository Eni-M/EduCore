namespace EduCore.Dtos;

public record PaginationQueryDto(int Page = 1, int PageSize = 20);

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

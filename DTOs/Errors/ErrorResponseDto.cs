namespace EduCore.Dtos.Errors;

public record ErrorResponseDto(
    int StatusCode,
    DateTime Timestamp,
    string Message,
    string? Error = null,
    string? TraceId = null,
    string? Path = null,
    IReadOnlyList<FieldError>? FieldErrors = null
);

public record FieldError
{
    public required string Field { get; init; }
    public string? Message { get; init; }
}

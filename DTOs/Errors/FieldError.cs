namespace EduCore.Dtos.Errors;

// One field-level validation failure; sent as the fieldErrors extension of a problem response.
public record FieldError
{
    public required string Field { get; init; }
    public string? Message { get; init; }
}

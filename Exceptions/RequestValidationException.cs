using InventoryService.DTOs.Records.Errors;

namespace InventoryService.Exceptions;

public class RequestValidationException : Exception
{
    public RequestValidationException(IReadOnlyList<FieldError> fieldErrors)
        : base("One or more fields are invalid.")
        => FieldErrors = fieldErrors;

    public IReadOnlyList<FieldError> FieldErrors { get; }
}

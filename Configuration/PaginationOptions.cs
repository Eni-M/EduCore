using Microsoft.Extensions.Options;

namespace EduCore.Configuration;

// The ceiling on how much one request may ask for. A caller controls page size, so this is the
// only thing standing between a list endpoint and "give me everything".
public class PaginationOptions
{
    public const string SectionName = "Pagination";

    public int DefaultPageSize { get; set; } = 20;

    public int MaxPageSize { get; set; } = 100;

    // The audit log is the widest table in the database and its rows are the least useful in
    // bulk, so it is capped harder than everything else.
    public int AuditLogMaxPageSize { get; set; } = 20;
}

public class PaginationOptionsValidator : IValidateOptions<PaginationOptions>
{
    public ValidateOptionsResult Validate(string? name, PaginationOptions options)
    {
        if (options.MaxPageSize is < 1 or > 500)
            return ValidateOptionsResult.Fail(
                $"Pagination:MaxPageSize ({options.MaxPageSize}) must be between 1 and 500. It is the "
                + "ceiling a caller cannot exceed; above 500 a single request can pull most of a table.");

        if (options.DefaultPageSize < 1 || options.DefaultPageSize > options.MaxPageSize)
            return ValidateOptionsResult.Fail(
                $"Pagination:DefaultPageSize ({options.DefaultPageSize}) must be between 1 and "
                + $"MaxPageSize ({options.MaxPageSize}).");

        return options.AuditLogMaxPageSize is < 1 or > 500
            ? ValidateOptionsResult.Fail(
                $"Pagination:AuditLogMaxPageSize ({options.AuditLogMaxPageSize}) must be between 1 and 500.")
            : ValidateOptionsResult.Success;
    }
}

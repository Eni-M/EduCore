using Microsoft.Extensions.Options;

namespace EduCore.Services.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    // Only "Local" is implemented today; the key exists so a cloud provider can be selected by
    // configuration later without a code change at the call sites.
    public string Provider { get; set; } = "Local";

    // Where LocalFileStorage keeps its files. Relative paths resolve against the app's working
    // directory. Kept out of source control (see .gitignore).
    public string LocalRoot { get; set; } = "storage";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    // An empty list means "accept anything"; a non-empty one is an allowlist enforced on upload.
    public string[] AllowedContentTypes { get; set; } = [];
}

public class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.LocalRoot))
            return ValidateOptionsResult.Fail("Storage:LocalRoot must be set.");

        if (options.MaxFileSizeBytes < 1)
            return ValidateOptionsResult.Fail(
                $"Storage:MaxFileSizeBytes ({options.MaxFileSizeBytes}) must be at least 1.");

        return ValidateOptionsResult.Success;
    }
}

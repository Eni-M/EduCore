using EduCore.Exceptions;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    // "Local" keeps files on the app's own disk; "S3" keeps them in a bucket. Anything that
    // outlives a single container needs S3 -- local disk on Render or in Docker is ephemeral.
    public string Provider { get; set; } = "Local";

    // Where LocalFileStorage keeps its files. Relative paths resolve against the app's working
    // directory. Kept out of source control (see .gitignore).
    public string LocalRoot { get; set; } = "storage";

    // S3 bucket name (not the ARN) and the region it lives in. Credentials are never configured
    // here: the AWS SDK resolves them from the environment or the instance role.
    public string S3Bucket { get; set; } = "";
    public string S3Region { get; set; } = "";

    // Optional key prefix, so one bucket can hold more than one environment's files.
    public string S3Prefix { get; set; } = "";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    // An empty list means "accept anything"; a non-empty one is an allowlist enforced on upload.
    public string[] AllowedContentTypes { get; set; } = [];

    public bool UsesS3 => Provider.Equals("S3", StringComparison.OrdinalIgnoreCase);
}

public class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (options.UsesS3)
        {
            if (string.IsNullOrWhiteSpace(options.S3Bucket))
                return ValidateOptionsResult.Fail("Storage:S3Bucket must be set when Storage:Provider is S3.");

            if (string.IsNullOrWhiteSpace(options.S3Region))
                return ValidateOptionsResult.Fail("Storage:S3Region must be set when Storage:Provider is S3.");
        }
        else if (string.IsNullOrWhiteSpace(options.LocalRoot))
        {
            return ValidateOptionsResult.Fail("Storage:LocalRoot must be set.");
        }

        if (options.MaxFileSizeBytes < 1)
            return ValidateOptionsResult.Fail(
                $"Storage:MaxFileSizeBytes ({options.MaxFileSizeBytes}) must be at least 1.");

        return ValidateOptionsResult.Success;
    }
}

public static class UploadRules
{
    // Every upload path applies these, so a file one endpoint accepts is not rejected by
    // another for a rule the first never checked.
    public static void EnsureAccepted(this StorageOptions options, long sizeBytes, string? contentType)
    {
        if (sizeBytes <= 0)
            throw new BusinessException("The uploaded file is empty.");

        if (sizeBytes > options.MaxFileSizeBytes)
            throw new BusinessException(
                $"The file is larger than the {options.MaxFileSizeBytes / (1024 * 1024)} MB limit.");

        if (options.AllowedContentTypes.Length > 0
            && !options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException($"Files of type '{contentType}' are not allowed.");
    }
}

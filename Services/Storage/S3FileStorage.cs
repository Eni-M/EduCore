using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace EduCore.Services.Storage;

// Keeps files in an S3 bucket instead of on the machine the app runs on, so a restarted or
// replicated container does not lose them. Keys are the same shape as the local backend's
// ("<school>/<student>/<guid>.pdf"), which is what lets the two be swapped by configuration.
public class S3FileStorage(IAmazonS3 client, IOptions<StorageOptions> options) : IFileStorage
{
    private readonly string _bucket = options.Value.S3Bucket;
    private readonly string _prefix = options.Value.S3Prefix.Trim('/');

    public async Task<string> SaveAsync(Stream content, string relativeDirectory, string extension, CancellationToken ct)
    {
        var key = $"{relativeDirectory.Trim('/')}/{Guid.NewGuid():N}{extension}";

        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = ObjectKey(key),
            InputStream = content,
            AutoCloseStream = false
        }, ct);

        return key;
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        try
        {
            var response = await client.GetObjectAsync(_bucket, ObjectKey(key), ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException("The stored file is missing.", key);
        }
    }

    // S3 delete is already idempotent, so a missing object is not an error here either.
    public Task DeleteAsync(string key, CancellationToken ct) =>
        client.DeleteObjectAsync(_bucket, ObjectKey(key), ct);

    private string ObjectKey(string key) => _prefix.Length == 0 ? key : $"{_prefix}/{key}";
}

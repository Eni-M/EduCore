namespace EduCore.Services.Interfaces;

// The one seam between a feature that keeps files and where those files actually live. Callers
// hold only the opaque key returned by SaveAsync; swapping the local implementation for a cloud
// one later changes nothing above this interface.
public interface IFileStorage
{
    // Persists the stream under a new key beneath relativeDirectory and returns that key.
    Task<string> SaveAsync(Stream content, string relativeDirectory, string extension, CancellationToken ct);

    Task<Stream> OpenReadAsync(string key, CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);
}

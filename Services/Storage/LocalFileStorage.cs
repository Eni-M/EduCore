using Microsoft.Extensions.Options;

namespace EduCore.Services.Storage;

// Stores files on the machine the app runs on, under a configured root. The key it hands back is
// a relative path (e.g. "<school>/<student>/<guid>.pdf"); every read and delete re-resolves that
// key against the root and refuses anything that would escape it.
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.LocalRoot);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string relativeDirectory, string extension, CancellationToken ct)
    {
        var key = $"{relativeDirectory.Trim('/', '\\')}/{Guid.NewGuid():N}{extension}";
        var full = ResolveWithinRoot(key);

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using var file = File.Create(full);
        await content.CopyToAsync(file, ct);

        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        var full = ResolveWithinRoot(key);

        if (!File.Exists(full))
            throw new FileNotFoundException("The stored file is missing.", key);

        return Task.FromResult<Stream>(File.OpenRead(full));
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var full = ResolveWithinRoot(key);

        if (File.Exists(full))
            File.Delete(full);

        return Task.CompletedTask;
    }

    // A key must never resolve outside the storage root. Without this a crafted key like
    // "../../appsettings.json" would read or delete an arbitrary file on the host.
    private string ResolveWithinRoot(string key)
    {
        var full = Path.GetFullPath(Path.Combine(_root, key));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The resolved path escapes the storage root.");

        return full;
    }
}

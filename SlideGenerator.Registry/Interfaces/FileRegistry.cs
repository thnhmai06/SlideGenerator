using AsyncKeyedLock;
using SlideGenerator.Registry.Entities;

namespace SlideGenerator.Registry.Interfaces;

/// <summary>
///     Async registry specialized for file-backed resources.
/// </summary>
/// <typeparam name="T">The resource type stored by the registry.</typeparam>
public abstract class FileRegistry<T> where T : IDisposable
{
    private readonly AsyncKeyedLocker<string> _writeLocker = new();

    /// <summary>
    ///     Creates a fresh resource instance for <paramref name="filePath" /> and acquires the
    ///     appropriate lock (shared for reads, exclusive for writes).
    /// </summary>
    public async ValueTask<Lease<T>> AcquireAsync(
        string filePath,
        bool writeable,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        
        IDisposable? releaser = null;
        if (writeable)
            releaser = await _writeLocker.LockAsync(normalizedPath, cancellationToken).ConfigureAwait(false);
        
        var value = CreateInstance(normalizedPath, writeable);
        return new Lease<T>(releaser, value);
    }

    protected abstract T CreateInstance(string filePath, bool isWritable);
}
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SlideGenerator.Application.Common;

/// <summary>
///     Provides a shared, reference-counted registry for reusable resources.
/// </summary>
/// <typeparam name="T">The resource type stored by the registry.</typeparam>
public abstract class Registry<T> : IDisposable
{
    /// <summary>
    ///     Represents a cached resource entry together with its current lifecycle metadata.
    /// </summary>
    protected sealed class Entry(T resource, bool isEditable)
    {
        /// <summary>
        ///     Gets or sets the cached resource instance.
        /// </summary>
        public T Resource { get; set; } = resource;

        /// <summary>
        ///     Gets or sets a value indicating whether the cached resource was opened in editable mode.
        /// </summary>
        public bool IsEditable { get; set; } = isEditable;

        /// <summary>
        ///     Gets or sets the number of active consumers currently using the cached resource.
        /// </summary>
        public int ReferenceCount { get; set; } = 1;
    }

    private readonly Lock _syncRoot = new();
    private readonly ConcurrentDictionary<string, Entry> _resources =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets an existing resource for the specified file path or opens a new one when none is cached.
    /// </summary>
    /// <param name="filePath">The file path used as the registry key.</param>
    /// <param name="isEditable">A value indicating whether the resource should be opened in editable mode.</param>
    /// <returns>The cached or newly opened resource instance.</returns>
    public virtual T GetOrOpen(string filePath, bool isEditable)
    {
        var normalizedPath = NormalizeKey(filePath);

        lock (_syncRoot)
        {
            if (_resources.TryGetValue(normalizedPath, out var existing))
            {
                if (existing.ReferenceCount == 0 && ShouldReplace(existing, isEditable))
                {
                    DisposeResource(existing.Resource);
                    existing.Resource = OpenResource(normalizedPath, isEditable);
                    existing.IsEditable = isEditable;
                }

                existing.ReferenceCount++;
                return existing.Resource;
            }

            var resource = OpenResource(normalizedPath, isEditable);
            _resources[normalizedPath] = new Entry(resource, isEditable);
            return resource;
        }
    }

    /// <summary>
    ///     Tries to retrieve a cached resource without changing its reference count.
    /// </summary>
    /// <param name="filePath">The file path used as the registry key.</param>
    /// <param name="resource">When this method returns, contains the cached resource if it exists; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a cached resource was found; otherwise, <c>false</c>.</returns>
    public virtual bool TryGet(string filePath, [MaybeNullWhen(false)] out T? resource)
    {
        var normalizedPath = NormalizeKey(filePath);
        if (_resources.TryGetValue(normalizedPath, out var entry))
        {
            resource = entry.Resource;
            return true;
        }

        resource = default;
        return false;
    }

    /// <summary>
    ///     Releases one reference to the resource associated with the specified file path.
    /// </summary>
    /// <param name="filePath">The file path used as the registry key.</param>
    /// <returns><c>true</c> if the registry contained the key; otherwise, <c>false</c>.</returns>
    public virtual bool Close(string filePath)
    {
        var normalizedPath = NormalizeKey(filePath);

        lock (_syncRoot)
        {
            if (!_resources.TryGetValue(normalizedPath, out var entry))
                return false;

            entry.ReferenceCount = Math.Max(0, entry.ReferenceCount - 1);
            if (entry.ReferenceCount > 0)
                return true;

            if (_resources.TryRemove(normalizedPath, out var removed))
                DisposeResource(removed.Resource);

            return true;
        }
    }

    /// <summary>
    ///     Opens or reuses a resource and returns a lease that releases the resource when disposed.
    /// </summary>
    /// <param name="filePath">The file path used as the registry key.</param>
    /// <param name="isEditable">A value indicating whether the resource should be opened in editable mode.</param>
    /// <returns>A lease that must be disposed when the caller is finished using the resource.</returns>
    public RegistryLease<T> Acquire(string filePath, bool isEditable)
    {
        var resource = GetOrOpen(filePath, isEditable);
        return new RegistryLease<T>(resource, () => Close(filePath));
    }

    /// <summary>
    ///     Disposes every cached resource currently held by the registry.
    /// </summary>
    public void Dispose()
    {
        lock (_syncRoot)
        {
            foreach (var key in _resources.Keys.ToList())
            {
                if (_resources.TryRemove(key, out var removed))
                    DisposeResource(removed.Resource);
            }
        }
    }

    /// <summary>
    ///     Normalizes a file path into the key used by the registry.
    /// </summary>
    /// <param name="filePath">The original file path.</param>
    /// <returns>The normalized registry key.</returns>
    protected virtual string NormalizeKey(string filePath)
    {
        return Path.GetFullPath(filePath);
    }

    /// <summary>
    ///     Opens a new resource instance for the specified normalized key.
    /// </summary>
    /// <param name="normalizedPath">The normalized registry key.</param>
    /// <param name="isEditable">A value indicating whether the resource should be opened in editable mode.</param>
    /// <returns>The newly opened resource instance.</returns>
    protected abstract T OpenResource(string normalizedPath, bool isEditable);

    /// <summary>
    ///     Determines whether an inactive cached resource can be replaced by a newly opened one.
    /// </summary>
    /// <param name="existing">The cached entry that is being reconsidered.</param>
    /// <param name="isEditable">A value indicating whether the caller requested editable access.</param>
    /// <returns><c>true</c> if the registry should replace the cached resource; otherwise, <c>false</c>.</returns>
    protected virtual bool ShouldReplace(Entry existing, bool isEditable)
    {
        return false;
    }

    /// <summary>
    ///     Disposes a resource instance if it supports <see cref="IDisposable"/>.
    /// </summary>
    /// <param name="resource">The resource to dispose.</param>
    protected virtual void DisposeResource(T resource)
    {
        if (resource is IDisposable disposable)
            disposable.Dispose();
    }
}
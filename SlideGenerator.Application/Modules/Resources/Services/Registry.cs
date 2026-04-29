using SlideGenerator.Application.Modules.Resources.Entities;
using SlideGenerator.Application.Modules.Resources.Interfaces;

namespace SlideGenerator.Application.Modules.Resources.Services;

/// <summary>
///     Async-first resource registry that creates a fresh resource instance on every acquiring via
///     a caller-supplied factory, and enforces reader-writer locking per key.
/// </summary>
/// <remarks>
///     <para>
///         Unlike a caching registry, every call to <see cref="AcquireAsync" /> invokes the factory
///         and produces a new resource instance. Sharing across calls within the same workflow run
///         is the responsibility of the workflow data's lease dictionaries.
///     </para>
///     <para>
///         Read acquires are shared — multiple callers may hold a read lock on the same key
///         simultaneously. Write acquires are exclusive — only one writer may hold the lock at a time,
///         and no reader may hold it concurrently.
///     </para>
///     <para>
///         Disposing the returned <see cref="Lease{TValue}" /> releases the lock and disposes the resource
///         (if it implements <see cref="IDisposable" />).
///     </para>
/// </remarks>
/// <typeparam name="TKey">The key type used to identify entries.</typeparam>
/// <typeparam name="TValue">The resource type managed by each lease.</typeparam>
/// <param name="acquireRead">Acquires a shared read lock for the given key.</param>
/// <param name="acquireWrite">Acquires an exclusive write lock for the given key.</param>
public class Registry<TKey, TValue>(
    Func<TKey, CancellationToken, ValueTask<ILock>> acquireRead,
    Func<TKey, CancellationToken, ValueTask<ILock>> acquireWrite)
    where TKey : notnull
{
    /// <summary>Normalises <paramref name="rawKey" /> before it is used as a registry key.</summary>
    protected virtual TKey FormatKey(TKey rawKey) => rawKey;

    /// <summary>
    ///     Creates a new resource instance via <paramref name="factory" /> and acquires a reader-writer
    ///     lock for <paramref name="key" />, then returns a lease that owns both.
    /// </summary>
    /// <remarks>
    ///     The factory is always invoked — there is no caching at this layer. The lock is acquired
    ///     after the factory completes so that file-open I/O never runs inside a lock wait.
    ///     If either the factory or the lock acquisition fails, no lease is returned and any partially
    ///     created resource is disposed of.
    /// </remarks>
    /// <param name="key">The key identifying the resource.</param>
    /// <param name="factory">Produces a fresh resource instance for <paramref name="key" />.</param>
    /// <param name="isWritable">
    ///     <see langword="true" /> to acquire an exclusive write lock;
    ///     <see langword="false" /> to acquire a shared read lock.
    /// </param>
    /// <param name="cancellationToken">Cancels the lock wait.</param>
    /// <returns>An <see cref="Lease{TValue}" /> whose <see cref="Lease{TValue}.Value" /> is the fresh resource instance.</returns>
    protected async ValueTask<Lease<TValue>> AcquireAsync(
        TKey key,
        Func<TKey, ValueTask<TValue>> factory,
        bool isWritable,
        CancellationToken cancellationToken = default)
    {
        key = FormatKey(key);

        var value = await factory(key).ConfigureAwait(false);

        ILock handle;
        try
        {
            handle = isWritable
                ? await acquireWrite(key, cancellationToken).ConfigureAwait(false)
                : await acquireRead(key, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            (value as IDisposable)?.Dispose();
            throw;
        }

        return new Lease<TValue>(handle, value);
    }
}
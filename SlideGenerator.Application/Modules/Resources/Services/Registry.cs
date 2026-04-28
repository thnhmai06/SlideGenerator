using SlideGenerator.Application.Modules.Resources.Abstractions;

namespace SlideGenerator.Application.Modules.Resources.Services;

/// <summary>
///     Thread-safe, async-first registry that creates resources on demand via a caller-supplied
///     factory, caches them per key, and disposes each resource (together with its slim) automatically
///     when the last holder releases its <see cref="IKeyedLockHandle" />.
/// </summary>
/// <typeparam name="TKey">The key type used to identify entries.</typeparam>
/// <typeparam name="TValue">The resource type managed by each entry.</typeparam>
/// <param name="locker">
///     Per-key locker that creates and owns one <see cref="System.Threading.SemaphoreSlim" /> per
///     live key.  The locker frees the slim (and its key) once all holders have released.
///     Concurrency limits (e.g., <see cref="int.MaxValue" /> for unrestricted access) are
///     configured on the locker at construction time, not per-call.
/// </param>
/// <param name="comparer">Optional equality comparer for <typeparamref name="TKey" />.</param>
public class Registry<TKey, TValue>(
    IAsyncKeyedLocker<TKey> locker,
    IEqualityComparer<TKey>? comparer = null) : IDisposable
    where TKey : notnull
{
    private readonly Dictionary<TKey, Entry> _entries =
        comparer is null ? [] : new Dictionary<TKey, Entry>(comparer);

    private readonly Lock _lock = new();

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var entry in _entries.Values)
                DisposeEntryResource(entry);
            _entries.Clear();
        }
    }

    /// <summary>Normalises <paramref name="rawKey" /> before it is used as a registry key.</summary>
    protected virtual TKey NormalizeKey(TKey rawKey)
    {
        return rawKey;
    }

    /// <summary>
    ///     Acquires the lock for <paramref name="key" /> and returns a lease over its cached resource.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The factory is invoked exactly once per live entry (inside the internal sync lock, so
    ///         it must not itself re-acquire the lock).  The resulting
    ///         <see cref="Task{TValue}" /> is shared by all concurrent callers for the same key.
    ///     </para>
    ///     <para>
    ///         If the factory task faults, the entry is evicted immediately, so the next call retries
    ///         the factory.
    ///     </para>
    ///     <para>
    ///         Disposing the returned lease releases the slim permit.  When the last pending user
    ///         exits, the entry is removed and the resource is disposed (if it implements
    ///         <see cref="IDisposable" />).
    ///     </para>
    /// </remarks>
    /// <param name="key">The key identifying the entry.</param>
    /// <param name="factory">
    ///     Produces the resource for a brand-new entry.  Its synchronous prelude runs inside
    ///     the internal sync lock; any async continuation runs outside.
    /// </param>
    /// <param name="cancellationToken">Cancels the slim wait.</param>
    /// <returns>A <see cref="Lease" /> whose <see cref="Lease.Value" /> is the cached resource.</returns>
    protected async ValueTask<Lease> AcquireAsync(
        TKey key,
        Func<TKey, ValueTask<TValue>> factory,
        CancellationToken cancellationToken = default)
    {
        key = NormalizeKey(key);

        Entry entry;
        lock (_lock)
        {
            if (!_entries.TryGetValue(key, out entry!))
            {
                // factory(key) starts the async work; only its synchronous prelude runs here.
                entry = new Entry(factory(key).AsTask());
                _entries[key] = entry;
            }

            entry.PendingUsers++;
        }

        IKeyedLockHandle handle;
        try
        {
            handle = await locker
                .LockAsync(key, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            lock (_lock)
            {
                DecrementAndCleanup(key, entry);
            }

            throw;
        }

        TValue value;
        try
        {
            value = await entry.ResourceTask.ConfigureAwait(false);
        }
        catch
        {
            handle.Dispose();
            lock (_lock)
            {
                // Evict faulted entry so the next caller gets a fresh factory call.
                if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                    _entries.Remove(key);
                --entry.PendingUsers;
            }

            throw;
        }

        return new Lease(this, key, entry, handle, value);
    }

    private void Release(TKey key, Entry entry, IKeyedLockHandle handle)
    {
        handle.Dispose(); // releases slim permit; locker disposes slim when the count returns to max
        lock (_lock)
        {
            DecrementAndCleanup(key, entry);
        }
    }

    /// <summary>Decrements <see cref="Entry.PendingUsers" /> and tears down the entry when it hits zero.</summary>
    /// <remarks>Caller must hold <c>_lock</c>.</remarks>
    private void DecrementAndCleanup(TKey key, Entry entry)
    {
        if (--entry.PendingUsers != 0) return;

        if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
            _entries.Remove(key);

        DisposeEntryResource(entry);
    }

    private static void DisposeEntryResource(Entry entry)
    {
        if (entry.ResourceTask is { IsCompletedSuccessfully: true, Result: IDisposable d })
            d.Dispose();
    }

    /// <summary>Pairs the resource creation task with the active pending-user count.</summary>
    internal sealed class Entry(Task<TValue> resourceTask)
    {
        /// <summary>
        ///     Completes with the resource once the factory finishes.  Faulted if the factory threw.
        /// </summary>
        public readonly Task<TValue> ResourceTask = resourceTask;

        /// <summary>
        ///     Number of callers that have incremented this count but have not yet released —
        ///     includes threads waiting for the slim <em>and</em> threads actively holding it.
        /// </summary>
        public int PendingUsers;
    }

    /// <summary>
    ///     A disposable lease over a registry-managed resource.
    ///     Dispose releases the slim permit; when the last holder disposes, the resource is disposed
    ///     and the entry is evicted from the registry.
    /// </summary>
    public sealed class Lease : IDisposable
    {
        private readonly Entry _entry;
        private readonly IKeyedLockHandle _handle;
        private readonly TKey _key;
        private readonly Registry<TKey, TValue> _owner;
        private int _disposed;

        internal Lease(Registry<TKey, TValue> owner, TKey key, Entry entry, IKeyedLockHandle handle, TValue value)
        {
            _owner = owner;
            _key = key;
            _entry = entry;
            _handle = handle;
            Value = value;
        }

        /// <summary>Gets the leased resource instance.</summary>
        public TValue Value { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _owner.Release(_key, _entry, _handle);
        }
    }
}
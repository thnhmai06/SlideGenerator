/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: Pool.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Coordinator.Application.Abstractions;

/// <summary>
///     Thread-safe object pool that creates instances on demand up to a runtime-resolved limit
///     and automatically disposes excess instances when the limit is reduced (scale-down).
/// </summary>
/// <typeparam name="T">The pooled resource type. Must be <see cref="IDisposable" />.</typeparam>
/// <param name="factory">Creates a new instance when the pool is empty and below the limit.</param>
/// <param name="limitResolver">
///     Returns the current maximum number of instances the pool may own.
///     <c>0</c> means unlimited. Called on every acquiring and release.
/// </param>
public abstract class Pool<T>(Func<T> factory, Func<uint> limitResolver) : IDisposable
    where T : IDisposable
{
    private readonly Stack<T> _free = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Lock _sync = new();
    private bool _disposed;
    private int _total;

    /// <summary>
    ///     Disposes all idle instances currently held in the free list.
    ///     Instances that are checked out remain alive until their callers release them.
    /// </summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            while (_free.Count > 0) _free.Pop().Dispose();
        }

        _signal.Dispose();
    }

    /// <summary>
    ///     Acquires an instance from the pool. Returns a free instance immediately if available;
    ///     creates a new one if below the limit; otherwise waits asynchronously until one is released.
    /// </summary>
    /// <param name="ct">Token used to cancel a waiting acquiring.</param>
    protected async ValueTask<T> AcquireAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var shouldCreate = false;

            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                var limit = (int)limitResolver();

                if (_free.Count > 0) return _free.Pop();
                if (limit == 0 || _total < limit)
                {
                    _total++;
                    shouldCreate = true;
                }
            }

            if (shouldCreate)
                try
                {
                    return factory();
                }
                catch
                {
                    lock (_sync)
                    {
                        _total--;
                    }

                    _signal.Release();
                    throw;
                }

            await _signal.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Returns an instance to the pool. If the current total exceeds the limit, the instance is
    ///     disposed of immediately (scale-down) rather than being re-pooled.
    /// </summary>
    /// <param name="instance">The instance to release.</param>
    protected void Release(T instance)
    {
        T? toDispose = default;
        bool wasDisposed;
        lock (_sync)
        {
            wasDisposed = _disposed;
            if (wasDisposed)
            {
                toDispose = instance;
            }
            else
            {
                var limit = (int)limitResolver();
                if (limit != 0 && _total > limit)
                {
                    _total--;
                    toDispose = instance;
                }
                else
                {
                    _free.Push(instance);
                }
            }
        }

        toDispose?.Dispose();
        if (!wasDisposed) _signal.Release();
    }

    /// <summary>
    ///     Trims excess idle instances when the limit has been reduced. Callers may hook this method
    ///     to a settings-changed event to apply scale-down eagerly rather than waiting for the next
    ///     <see cref="Release" />.
    /// </summary>
    public Task AdjustAsync()
    {
        var trimmed = 0;
        lock (_sync)
        {
            if (_disposed) return Task.CompletedTask;
            var limit = (int)limitResolver();
            if (limit == 0) return Task.CompletedTask;

            while (_total > limit && _free.Count > 0)
            {
                _free.Pop().Dispose();
                _total--;
                trimmed++;
            }
        }

        for (var i = 0; i < trimmed; i++) _signal.Release();
        return Task.CompletedTask;
    }
}
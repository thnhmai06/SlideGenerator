/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: BufferedRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Generator.Persistence;

/// <summary>
///     Coalesce-and-flush base: callers <see cref="Enqueue" /> dirty values keyed by <typeparamref name="TKey" />
///     (last write per key wins); a background loop atomically drains the buffer roughly once a second and
///     calls <see cref="UpsertBatchAsync" /> once per tick, then raises <see cref="Flushed" /> with the
///     batch. Extracted from the pattern used by the old Stdio-side progress coalescer so any table that
///     often changes (here, just <c>Jobs</c>) can reuse the same buffering without re-deriving it.
/// </summary>
internal abstract class BufferedRepository<TKey, TValue> : IAsyncDisposable where TKey : notnull
{
    private const uint FlushIntervalSeconds = 1;
    private readonly CancellationTokenSource _cts = new();

    private readonly ILogger _logger;
    private readonly Task _loop;
    private ConcurrentDictionary<TKey, TValue> _dirty = new();

    protected BufferedRepository(ILogger logger)
    {
        _logger = logger;
        _loop = RunFlushLoopAsync(_cts.Token);
    }

    /// <summary>Cancels the flush loop and flushes one last time, so nothing accumulated since the previous tick is dropped.</summary>
    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }

        try
        {
            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Final flush on dispose failed.");
        }

        _cts.Dispose();
    }

    /// <summary>Marks <paramref name="value" /> dirty under <paramref name="key" /> — last write wins.</summary>
    public void Enqueue(TKey key, TValue value)
    {
        _dirty[key] = value;
    }

    /// <summary>Rose after each successful flush with the batch that was just persisted.</summary>
    public event Action<IReadOnlyList<TValue>>? Flushed;

    /// <summary>Persists one batch of dirty values in a single round-trip.</summary>
    protected abstract Task UpsertBatchAsync(IReadOnlyList<TValue> batch, CancellationToken ct);

    /// <summary>Atomically drains every dirty value and persists it immediately, bypassing the ~1s tick.</summary>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        var old = Interlocked.Exchange(ref _dirty, new ConcurrentDictionary<TKey, TValue>());
        if (old.IsEmpty) return;

        var batch = old.Values.ToList();
        await UpsertBatchAsync(batch, ct).ConfigureAwait(false);
        Flushed?.Invoke(batch);
    }

    private async Task RunFlushLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(FlushIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                try
                {
                    await FlushAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One failed flush (DB I/O, etc.) must not permanently stop delivery of the following
                    // updates for the rest of the process lifetime.
                    _logger.LogWarning(ex, "Buffered flush failed; will retry on the next tick.");
                }
        }
        catch (OperationCanceledException)
        {
            // Expected on DisposeAsync.
        }
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobGate.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     The sole concurrency/RAM control: caps how many jobs run at once across <b>all</b> workflows, since each
///     running job keeps a workbook/presentation open in memory. The limit is re-read on every
///     <see cref="AcquireAsync" />, so a settings change applies to jobs that start afterwards.
/// </summary>
/// <remarks>
///     When the limit changes, a <b>new</b> semaphore is swapped in rather than resized, so jobs already waiting on
///     (or holding a slot of) the previous instance are unaffected — only newly queued jobs see the new limit.
///     Each <see cref="Lease" /> releases the exact semaphore it acquired from, never the current field, so a swap
///     mid-job cannot cause a <see cref="SemaphoreFullException" />.
/// </remarks>
/// <param name="maxConcurrentJobs">Supplies the current limit; values below 1 are treated as 1.</param>
internal sealed class JobGate(Func<int> maxConcurrentJobs)
{
    private readonly Lock _lock = new();
    private int _currentLimit;
    private SemaphoreSlim? _semaphore;

    /// <summary>Waits for a free slot and returns the lease that gives it back when disposed.</summary>
    public async Task<IDisposable> AcquireAsync(CancellationToken ct)
    {
        SemaphoreSlim semaphore;
        lock (_lock)
        {
            var limit = Math.Max(1, maxConcurrentJobs());
            if (_semaphore is null || limit != _currentLimit)
            {
                _currentLimit = limit;
                _semaphore = new SemaphoreSlim(limit, limit);
            }

            semaphore = _semaphore;
        }

        await semaphore.WaitAsync(ct).ConfigureAwait(false);
        return new Lease(semaphore);
    }

    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0) semaphore.Release();
        }
    }
}

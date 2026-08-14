/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: JobEngine.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Jobs.Engine;

/// <summary>
///     Runs jobs concurrently — bounded by <see cref="IJobConcurrencyProvider" />, tracked by
///     <typeparamref name="TKey" /> — without knowing what a job does or what <typeparamref name="TState" />
///     means. See <see cref="IJobWorkload{TState}" /> for the domain-specific half of the contract.
/// </summary>
public interface IJobEngine<TKey, TState> where TKey : notnull
{
    /// <summary>
    ///     Schedules every job returned by <paramref name="resumeSource" /> and returns — it does not wait
    ///     for any of them to finish. Call once at startup.
    /// </summary>
    Task InitializeAsync(IJobResumeSource<TKey, TState> resumeSource, CancellationToken ct = default);

    /// <summary>
    ///     Cancels every running job, unblocks any that are paused, and waits for all of them to unwind
    ///     before returning — guaranteeing no workload is still calling
    ///     <see cref="IJobContext{TState}.ReportAsync" /> once this completes. Call at shutdown.
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers and starts a fresh job. Throws <see cref="InvalidOperationException" /> if
    ///     <paramref name="key" /> is already registered — a duplicate key is a caller bug, not a state to
    ///     silently tolerate.
    /// </summary>
    Task StartJobAsync(TKey key, TState initialState, IJobWorkload<TState> workload, CancellationToken ct = default);

    /// <summary>Pauses a running job at its next checkpoint. Returns <see langword="false" /> if not currently registered.</summary>
    Task<bool> PauseAsync(TKey key);

    /// <summary>Resumes a paused job. Returns <see langword="false" /> if not currently registered.</summary>
    Task<bool> ResumeAsync(TKey key);

    /// <summary>Cancels a running or paused job. Returns <see langword="false" /> if not currently registered.</summary>
    Task<bool> StopAsync(TKey key);
}

/// <summary>
///     Runs each job's <see cref="IJobWorkload{TState}" /> on its own <see cref="Task.Run(Action)" />, gated
///     by a concurrency semaphore sized from <see cref="IJobConcurrencyProvider" />. See
///     <see cref="IJobEngine{TKey,TState}" /> for the full contract — this type knows nothing about what a
///     job does, or what <typeparamref name="TState" />/<typeparamref name="TKey" /> mean.
/// </summary>
public sealed class JobEngine<TKey, TState>(
    IJobConcurrencyProvider concurrencyProvider,
    IJobObserver<TKey, TState> observer,
    ILogger<JobEngine<TKey, TState>> logger) : IJobEngine<TKey, TState> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, RunningJob> _running = new();
    private int _currentLimit = 1;
    private SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc />
    public async Task InitializeAsync(IJobResumeSource<TKey, TState> resumeSource, CancellationToken ct = default)
    {
        ApplyConcurrencyLimit();

        var pending = await resumeSource.GetPendingJobsAsync(ct).ConfigureAwait(false);
        foreach (var job in pending)
        {
            var running = Register(job.Key, job.State);
            running.RunTask = Task.Run(
                () => RunJobAsync(job.Key, job.State, job.Workload, true, running), CancellationToken.None);
        }

        logger.LogInformation("JobEngine initialized. Resumed {Count} pending job(s).", pending.Count);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        foreach (var job in _running.Values)
        {
            await job.Cts.CancelAsync().ConfigureAwait(false);
            job.Gate.Resume(); // unblock a paused checkpoint so cancellation is observed promptly
        }

        await Task.WhenAll(_running.Values.Select(j => j.RunTask ?? Task.CompletedTask))
            .ContinueWith(_ => { }, TaskScheduler.Default).ConfigureAwait(false);
        logger.LogInformation("JobEngine shut down.");
    }

    /// <inheritdoc />
    public Task StartJobAsync(
        TKey key, TState initialState, IJobWorkload<TState> workload, CancellationToken ct = default)
    {
        ApplyConcurrencyLimit();

        var running = Register(key, initialState);
        running.RunTask = Task.Run(
            () => RunJobAsync(key, initialState, workload, false, running), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> PauseAsync(TKey key)
    {
        if (!_running.TryGetValue(key, out var job)) return false;
        job.Gate.Pause();
        await observer.OnPausedAsync(key, job.GetLastState(), CancellationToken.None).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ResumeAsync(TKey key)
    {
        if (!_running.TryGetValue(key, out var job)) return false;
        job.Gate.Resume();
        await observer.OnResumedAsync(key, job.GetLastState(), CancellationToken.None).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public Task<bool> StopAsync(TKey key)
    {
        if (!_running.TryGetValue(key, out var job)) return Task.FromResult(false);
        job.Cts.Cancel();
        job.Gate.Resume(); // unblock a paused checkpoint so cancellation is observed promptly
        return Task.FromResult(true);
    }

    /// <summary>
    ///     Re-applies <see cref="IJobConcurrencyProvider.MaxConcurrentJobs" /> onto <see cref="_semaphore" />,
    ///     but only when the value actually changed since the last apply — swapping unconditionally on
    ///     every call would give each newly-started job its own private, uncontended semaphore instead of
    ///     sharing the pool with jobs already running, defeating the limit entirely. When it does change,
    ///     a new instance is swapped in rather than mutating the old one, so jobs already waiting on the
    ///     previous instance are unaffected — only newly-queued waits see the new limit.
    /// </summary>
    private void ApplyConcurrencyLimit()
    {
        var value = concurrencyProvider.MaxConcurrentJobs;
        if (value == _currentLimit) return;
        _currentLimit = value;
        _semaphore = new SemaphoreSlim(value, value);
    }

    /// <summary>Registers a new running job. Throws if <paramref name="key" /> is already registered.</summary>
    private RunningJob Register(TKey key, TState initialState)
    {
        var job = new RunningJob(initialState);
        return _running.TryAdd(key, job)
            ? job
            : throw new InvalidOperationException($"Job '{key}' is already registered.");
    }

    /// <summary>
    ///     Runs one job to completion (or until paused/cancelled/faulted), publishing the initial "starting
    ///     execution" progress before the concurrency slot is acquired, then the terminal outcome on exit.
    /// </summary>
    private async Task RunJobAsync(
        TKey key, TState initialState, IJobWorkload<TState> workload, bool isResume, RunningJob running)
    {
        var ct = running.Cts.Token;

        await observer.OnProgressAsync(key, initialState, true, CancellationToken.None)
            .ConfigureAwait(false);

        // Captured once, up front: if a later StartJobAsync/InitializeAsync swaps _semaphore while this
        // job is in flight, Wait and Release below must still pair on the SAME instance — reading the
        // mutable field again at Release time could release a slot on a semaphore this job never
        // acquired from (SemaphoreFullException) or silently corrupt the new one's count.
        var semaphore = _semaphore;
        var acquired = false;
        try
        {
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            acquired = true;

            var context = new JobContext(isResume, running, observer, key);
            var finalState = await workload.RunAsync(initialState, context, ct).ConfigureAwait(false);
            await observer.OnTerminalAsync(key, new JobTerminalResult<TState>(JobOutcome.Completed, finalState, null),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (running.Cts.IsCancellationRequested)
        {
            await observer.OnTerminalAsync(key,
                new JobTerminalResult<TState>(JobOutcome.Cancelled, running.GetLastState(), null),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {Key} failed.", key);
            await observer.OnTerminalAsync(key,
                new JobTerminalResult<TState>(JobOutcome.Faulted, running.GetLastState(), ex),
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            if (acquired) semaphore.Release();
            _running.TryRemove(key, out _);
        }
    }

    /// <summary>In-memory registry entry for one currently running (or resumed) job.</summary>
    private sealed class RunningJob(TState initialState)
    {
        private readonly Lock _lock = new();
        private TState _lastState = initialState;

        public CancellationTokenSource Cts { get; } = new();
        public PauseGate Gate { get; } = new();
        public Task? RunTask { get; set; }

        public void SetLastState(TState state)
        {
            lock (_lock)
            {
                _lastState = state;
            }
        }

        public TState GetLastState()
        {
            lock (_lock)
            {
                return _lastState;
            }
        }
    }

    /// <summary>
    ///     Bridges a running job's <see cref="RunningJob" />/<see cref="IJobObserver{TKey,TState}" /> to the
    ///     <see cref="IJobContext{TState}" /> a workload sees.
    /// </summary>
    private sealed class JobContext(bool isResume, RunningJob running, IJobObserver<TKey, TState> observer, TKey key)
        : IJobContext<TState>
    {
        public bool IsResume { get; } = isResume;

        public async Task ReportAsync(TState state, bool durable = false, CancellationToken ct = default)
        {
            running.SetLastState(state);
            await observer.OnProgressAsync(key, state, durable, ct).ConfigureAwait(false);
        }

        public Task CheckpointAsync(CancellationToken ct)
        {
            return running.Gate.CheckpointAsync(ct);
        }
    }
}
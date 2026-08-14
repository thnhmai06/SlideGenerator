/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: IJobEngine.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

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

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: IJobWorkload.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>
///     The domain-specific work a job performs. A workload never knows where or when it runs, or how many
///     other jobs run alongside it — that is entirely <see cref="IJobEngine{TKey,TState}" />'s concern.
/// </summary>
public interface IJobWorkload<TState>
{
    /// <summary>
    ///     Runs the job to completion, or until <paramref name="ct" /> is cancelled. Implementations should
    ///     call <see cref="IJobContext{TState}.CheckpointAsync" /> between logical steps (never mid-step) to
    ///     honor pause, and <see cref="IJobContext{TState}.ReportAsync" /> to publish intermediate progress.
    /// </summary>
    /// <returns>The final state, once the job has fully completed.</returns>
    Task<TState> RunAsync(TState state, IJobContext<TState> context, CancellationToken ct);
}

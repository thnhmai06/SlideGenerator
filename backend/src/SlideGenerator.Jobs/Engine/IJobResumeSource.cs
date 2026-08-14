/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: IJobResumeSource.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>One job found non-terminal at startup, ready to resume.</summary>
public sealed record PendingJob<TKey, TState>(TKey Key, TState State, IJobWorkload<TState> Workload);

/// <summary>
///     Supplies the engine with the jobs to resume at startup (crash recovery). The engine has no idea how
///     these are found or reconstructed — that is entirely the consumer's concern.
/// </summary>
public interface IJobResumeSource<TKey, TState>
{
    Task<IReadOnlyList<PendingJob<TKey, TState>>> GetPendingJobsAsync(CancellationToken ct);
}

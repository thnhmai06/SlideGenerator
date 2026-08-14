/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: IJobObserver.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>
///     Consumer-supplied sink for job lifecycle transitions. The engine never inspects
///     <typeparamref name="TState" /> — it only forwards whatever the workload reported (or, for a terminal
///     transition, whatever <see cref="IJobEngine{TKey,TState}" /> itself decided — see
///     <see cref="JobTerminalResult{TState}" />). Only the engine decides when a transition is terminal; an
///     observer must not infer outcome on its own.
/// </summary>
public interface IJobObserver<in TKey, TState>
{
    /// <summary>
    ///     A job reported progress — including the "starting execution" tick published before a concurrency
    ///     slot is acquired, which is always <paramref name="durable" /> = <see langword="true" />.
    /// </summary>
    Task OnProgressAsync(TKey key, TState state, bool durable, CancellationToken ct);

    /// <summary>A running job was paused.</summary>
    Task OnPausedAsync(TKey key, TState state, CancellationToken ct);

    /// <summary>A paused job was resumed.</summary>
    Task OnResumedAsync(TKey key, TState state, CancellationToken ct);

    /// <summary>A job reached a terminal outcome (Completed/Cancelled/Faulted).</summary>
    Task OnTerminalAsync(TKey key, JobTerminalResult<TState> result, CancellationToken ct);
}
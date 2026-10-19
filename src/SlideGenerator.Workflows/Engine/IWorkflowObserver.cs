/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows
 * File: IWorkflowObserver.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Workflows.Engine;

/// <summary>
///     Consumer-supplied sink for workflow lifecycle transitions. The engine never inspects
///     <typeparamref name="TState" /> — it only forwards whatever the runner reported (or, for a terminal
///     transition, whatever <see cref="IWorkflowEngine{TKey,TState}" /> itself decided — see
///     <see cref="WorkflowTerminalResult{TState}" />). Only the engine decides when a transition is terminal; an
///     observer must not infer outcome on its own.
/// </summary>
public interface IWorkflowObserver<in TKey, TState>
{
    /// <summary>
    ///     A workflow reported progress — including the "starting execution" tick published before the runner
    ///     starts, which is always <paramref name="durable" /> = <see langword="true" />.
    /// </summary>
    Task OnProgressAsync(TKey key, TState state, bool durable, CancellationToken ct);

    /// <summary>A running workflow was paused.</summary>
    Task OnPausedAsync(TKey key, TState state, CancellationToken ct);

    /// <summary>A paused workflow was resumed.</summary>
    Task OnResumedAsync(TKey key, TState state, CancellationToken ct);

    /// <summary>A workflow reached a terminal outcome (Completed/Cancelled/Faulted).</summary>
    Task OnTerminalAsync(TKey key, WorkflowTerminalResult<TState> result, CancellationToken ct);
}
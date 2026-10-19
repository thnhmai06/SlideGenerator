/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows
 * File: IWorkflowRunner.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Workflows.Engine;

/// <summary>
///     The domain-specific work a workflow performs. A runner never knows where or when it runs, or how many
///     other workflows run alongside it — that is entirely <see cref="IWorkflowEngine{TKey,TState}" />'s concern.
/// </summary>
public interface IWorkflowRunner<TState>
{
    /// <summary>
    ///     Runs the workflow to completion, or until <paramref name="ct" /> is cancelled. Implementations should
    ///     call <see cref="IWorkflowContext{TState}.CheckpointAsync" /> between logical steps (never mid-step) to
    ///     honor pause, and <see cref="IWorkflowContext{TState}.ReportAsync" /> to publish intermediate progress.
    /// </summary>
    /// <returns>The final state, once the workflow has fully completed.</returns>
    Task<TState> RunAsync(TState state, IWorkflowContext<TState> context, CancellationToken ct);
}
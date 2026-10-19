/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows
 * File: IWorkflowResumeSource.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Workflows.Engine;

/// <summary>One workflow found non-terminal at startup, ready to resume.</summary>
public sealed record PendingWorkflow<TKey, TState>(TKey Key, TState State, IWorkflowRunner<TState> Runner);

/// <summary>
///     Supplies the engine with the workflows to resume at startup (crash recovery). The engine has no idea how
///     these are found or reconstructed — that is entirely the consumer's concern.
/// </summary>
public interface IWorkflowResumeSource<TKey, TState>
{
    Task<IReadOnlyList<PendingWorkflow<TKey, TState>>> GetPendingWorkflowsAsync(CancellationToken ct);
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows
 * File: WorkflowTerminalResult.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Workflows.Engine;

/// <summary>
///     A workflow's final outcome. For <see cref="WorkflowOutcome.Completed" />, <paramref name="State" /> is the
///     runner's own return value from <see cref="IWorkflowRunner{TState}.RunAsync" />. For
///     <see cref="WorkflowOutcome.Cancelled" />/<see cref="WorkflowOutcome.Faulted" /> (no return value, since the run
///     threw), it is the last state the runner passed to <see cref="IWorkflowContext{TState}.ReportAsync" /> —
///     never the initial state, and never reconstructed after the fact.
/// </summary>
public sealed record WorkflowTerminalResult<TState>(WorkflowOutcome Outcome, TState State, Exception? Exception);
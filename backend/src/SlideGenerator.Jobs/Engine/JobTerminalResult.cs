/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: JobTerminalResult.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>
///     A job's final outcome. For <see cref="JobOutcome.Completed" />, <paramref name="State" /> is the
///     workload's own return value from <see cref="IJobWorkload{TState}.RunAsync" />. For
///     <see cref="JobOutcome.Cancelled" />/<see cref="JobOutcome.Faulted" /> (no return value, since the run
///     threw), it is the last state the workload passed to <see cref="IJobContext{TState}.ReportAsync" /> —
///     never the initial state, and never reconstructed after the fact.
/// </summary>
public sealed record JobTerminalResult<TState>(JobOutcome Outcome, TState State, Exception? Exception);

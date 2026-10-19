/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowSnapshot.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Workflows.Models;

/// <summary>
///     Current-state row for one workflow: identity, status/phase/resume-position, and the resolved
///     specification needed to run or resume it — everything a workflow needs to survive a process restart
///     without re-reading the originating recipe (see <see cref="WorkflowSpecification" />). This same shape
///     also serves as the workflow-scoped progress notification payload published via <c>IEventBus</c> — there
///     is no separate "WorkflowProgress" DTO, since a workflow's current state IS its progress.
/// </summary>
/// <param name="RequestId">The request for this workflow belongs to.</param>
/// <param name="WorkflowId">Ordinal position of this workflow within the request (0-based) — not a GUID.</param>
/// <param name="WorkflowStatus">Current execution status.</param>
/// <param name="Phase">Current phase within the 4-phase pipeline.</param>
/// <param name="CurrentIndex">
///     How many rows have completed within <see cref="Phase" /> — the resume point. Reset to 0 on every
///     phase transition.
/// </param>
/// <param name="Specification">Everything needed to run or resume this workflow, fully resolved.</param>
/// <param name="Timestamp">UTC timestamp of the last state change.</param>
/// <param name="TotalRows">
///     The worksheet's row count (after <see cref="RowFilter" /> is applied), known from the moment the
///     runner starts running — <see langword="null" /> only for a snapshot minted before this field
///     existed (a crash-resumed workflow from an older build) or by a caller that never sets it. Determinate
///     progress (Runs' <c>ProgressBar</c>) falls back to indeterminate when this is <see langword="null" />.
/// </param>
public sealed record WorkflowSnapshot(
    string RequestId,
    int WorkflowId,
    WorkflowStatus WorkflowStatus,
    WorkflowPhase Phase,
    int CurrentIndex,
    WorkflowSpecification Specification,
    DateTimeOffset Timestamp,
    int? TotalRows = null)
{
    /// <summary>Gets the output file path this workflow writes to — a convenience shortcut onto <see cref="Specification" />.</summary>
    public string OutputPath => Specification.OutputPath;
}
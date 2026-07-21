/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Progress.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Domain.Models.Enum;

namespace SlideGenerator.Generator.Domain.Models.Data;

/// <summary>
///     Identifies the aggregate lifecycle phase of a generation request, derived across every job spawned
///     for it. Monotonically increasing — never regresses once reported.
/// </summary>
public enum RequestPhase
{
    /// <summary>At least one job has been spawned and is running its preparation step.</summary>
    PreparationStarted,

    /// <summary>Every known job of this request has left its preparation phase.</summary>
    ProcessingStarted,

    /// <summary>Every job of this request has reached a terminal state.</summary>
    Completed
}

/// <summary>Request-scoped progress notification — reports the aggregate lifecycle phase of a request.</summary>
public sealed record RequestProgress
{
    /// <summary>Gets the request id this progress belongs to.</summary>
    public required string RequestId { get; init; }

    /// <summary>Gets the aggregate lifecycle phase.</summary>
    public required RequestPhase Phase { get; init; }

    /// <summary>Gets the UTC timestamp when this event was emitted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>Job-scoped progress notification — reports the execution status of a single job.</summary>
public sealed record JobProgress
{
    /// <summary>Gets the request id this job belongs to.</summary>
    public required string RequestId { get; init; }

    /// <summary>Gets the WorkflowCore instance id of this job.</summary>
    public required string JobId { get; init; }

    /// <summary>Gets the current job execution status.</summary>
    public required Status Status { get; init; }

    /// <summary>Gets the UTC timestamp when this event was emitted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>Row-scoped progress notification — reports the processing status of a single data row.</summary>
public sealed record RowProgress
{
    /// <summary>Gets the request id this row belongs to.</summary>
    public required string RequestId { get; init; }

    /// <summary>Gets the job id this row belongs to.</summary>
    public required string JobId { get; init; }

    /// <summary>Gets the 1-based data row index (excludes the header row).</summary>
    public required int RowIndex { get; init; }

    /// <summary>Gets the current row processing status.</summary>
    public required RowStatus Status { get; init; }

    /// <summary>Gets the granular sub-action currently being performed for this row, if any.</summary>
    public RowStage Stage { get; init; } = RowStage.None;

    /// <summary>
    ///     Gets free-text context for <see cref="Stage" /> (e.g., the URL being downloaded, or the row's
    ///     failure message), or <see langword="null" /> if not applicable.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>Gets the UTC timestamp when this event was emitted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
}

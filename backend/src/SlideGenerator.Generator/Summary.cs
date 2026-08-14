/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Summary.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Generator;

/// <summary>
///     Lightweight snapshot of a generation request returned by <c>generating.active.list</c> and
///     <c>generating.completed.list</c> IPC methods, keyed by request id in the returned dictionary (see
///     <c>IService.ListActiveAsync</c>/<c>ListCompletedAsync</c>) — not carried as a field here. Two
///     levels: request-level aggregate fields, and <see cref="Jobs" /> — one <see cref="JobSummary" /> per
///     job spawned for this request, keyed by job id. Row-level detail is no longer persisted — a job's
///     resume position is <see cref="JobSummary.CurrentIndex" /> within <see cref="JobSummary.Phase" />;
///     live row-by-row progress is only available via the <c>progress/rows</c> JSON-RPC notification stream.
/// </summary>
public sealed record Summary
{
    /// <summary>Gets the original request submitted to <c>generating.active.create</c>.</summary>
    public required Request Request { get; init; }

    /// <summary>Gets the aggregate execution status across every job of this request (see <c>Service.DeriveStatus</c>).</summary>
    public required JobStatus JobStatus { get; init; }

    /// <summary>Gets the aggregate lifecycle phase of this request, or <see langword="null" /> if none recorded yet.</summary>
    public RequestPhase? Phase { get; init; }

    /// <summary>Gets the UTC timestamp when the earliest job of this request was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the UTC timestamp when the latest job finished or was terminated, if the request is complete/canceled.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets log lines scoped to the request itself (not attributable to any specific job) — typically
    ///     empty, since almost every log line produced during generation belongs to a job.
    /// </summary>
    public required IReadOnlyList<LogEntry> Logs { get; init; }

    /// <summary>Gets each job of this request, keyed by job id (its ordinal within the request).</summary>
    public required IReadOnlyDictionary<int, JobSummary> Jobs { get; init; }
}

/// <summary>
///     Lightweight snapshot of a single job within a <see cref="Summary" />. Carries only current-state
///     fields — per-row history is not persisted (see <see cref="JobPhase" />/<see cref="CurrentIndex" />
///     as the sole resume state); row-level detail is only available live via the <c>progress/rows</c>
///     JSON-RPC notification, not through this snapshot.
/// </summary>
public sealed record JobSummary
{
    /// <summary>Gets the execution status of this specific job.</summary>
    public required JobStatus JobStatus { get; init; }

    /// <summary>Gets the current phase within the 4-phase pipeline.</summary>
    public required JobPhase Phase { get; init; }

    /// <summary>Gets how many rows have completed within <see cref="Phase" /> — the resume point.</summary>
    public required int CurrentIndex { get; init; }

    /// <summary>Gets the output file path this job writes to.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Gets the UTC timestamp when this job finished or was terminated, if applicable.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Gets log lines scoped to this job (including its rows, which have no row-specific entry point anymore).</summary>
    public required IReadOnlyList<LogEntry> Logs { get; init; }
}

/// <summary>A single log line captured from the workflow log file, attributed to a Request/Job/Row scope.</summary>
public sealed record LogEntry
{
    /// <summary>Gets the timestamp when this line was written.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     Gets the scope path this line belongs to: <c>"&lt;requestId&gt;"</c>,
    ///     <c>"&lt;requestId&gt;/&lt;jobId&gt;"</c>, or <c>"&lt;requestId&gt;/&lt;jobId&gt;/&lt;rowIndex&gt;"</c>.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>Gets the log level abbreviation (e.g. <c>"INF"</c>, <c>"WRN"</c>, <c>"ERR"</c>).</summary>
    public required string Level { get; init; }

    /// <summary>Gets the rendered log message.</summary>
    public required string Info { get; init; }
}

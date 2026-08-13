/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobRecord.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Job;

/// <summary>
///     Current-state row for one job: identity, status/phase/resume-position, and the resolved
///     specification needed to run or resume it — everything a job needs to survive a process restart
///     without re-reading the originating recipe (see <see cref="JobSpecification" />). This same shape
///     also serves as the job-scoped progress notification payload published via <c>IEventBus</c> — there
///     is no separate "JobProgress" DTO, since a job's current state IS its progress.
/// </summary>
/// <param name="RequestId">The request this job belongs to.</param>
/// <param name="JobId">Ordinal position of this job within the request (0-based) — not a GUID.</param>
/// <param name="Status">Current execution status.</param>
/// <param name="Phase">Current phase within the 4-phase pipeline.</param>
/// <param name="CurrentIndex">
///     How many rows have completed within <see cref="Phase" /> — the resume point. Reset to 0 on every
///     phase transition.
/// </param>
/// <param name="Specification">Everything needed to run or resume this job, fully resolved.</param>
/// <param name="Timestamp">UTC timestamp of the last state change.</param>
public sealed record JobRecord(
    string RequestId,
    int JobId,
    Status Status,
    JobPhase Phase,
    int CurrentIndex,
    JobSpecification Specification,
    DateTimeOffset Timestamp)
{
    /// <summary>Gets the output file path this job writes to — a convenience shortcut onto <see cref="Specification" />.</summary>
    public string OutputPath => Specification.OutputPath;
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IJobsRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Models.Data;

namespace SlideGenerator.Generator.Abstractions;

/// <summary>
///     Buffered current-state store for the <c>Jobs</c> table — the only frequently-changing table left
///     (Requests are write-once, Recipes barely change). Writers call <see cref="Enqueue" /> as often as
///     they like; a background loop coalesces and flushes to SQLite roughly once per second.
/// </summary>
public interface IJobsRepository
{
    /// <summary>Marks <paramref name="record" /> dirty for the next flush — last write for a given key wins.</summary>
    void Enqueue(JobRecord record);

    /// <summary>Flushes every dirty record to storage immediately, bypassing the ~1s tick.</summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>Raised after each successful flush with the batch that was just persisted.</summary>
    event Action<IReadOnlyList<JobRecord>>? Flushed;

    /// <summary>Gets every job belonging to <paramref name="requestId" />, ordered by job id.</summary>
    Task<IReadOnlyList<JobRecord>> GetByRequestIdAsync(string requestId, CancellationToken ct = default);

    /// <summary>Gets every job across all requests whose status is not yet terminal (for crash recovery).</summary>
    Task<IReadOnlyList<JobRecord>> GetNonTerminalAsync(CancellationToken ct = default);

    /// <summary>Gets every job row across every request, regardless of status (for request-group listing).</summary>
    Task<IReadOnlyList<JobRecord>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes every job row belonging to <paramref name="requestId" />.</summary>
    Task DeleteByRequestIdAsync(string requestId, CancellationToken ct = default);
}

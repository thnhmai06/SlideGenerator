/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IStudioRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Domain.Models.Data;

namespace SlideGenerator.Generator.Application.Abstractions;

/// <summary>
///     Persists Request/Job/Row progress (current-state, UPSERT semantics) to the Studio SQLite database,
///     backing <c>Summary</c> queries and surviving process restart. Every upsert method is batch-shaped —
///     callers gather every dirty item up front and issue a single transaction instead of one round-trip
///     per item (mirrors <c>SqliteCache</c>'s pattern).
/// </summary>
public interface IStudioRepository
{
    /// <summary>Upserts a single request's aggregate phase.</summary>
    Task UpsertRequestAsync(RequestProgress progress, CancellationToken ct = default);

    /// <summary>Upserts every job progress entry in <paramref name="progress" /> in a single transaction.</summary>
    Task UpsertJobsAsync(IReadOnlyList<JobProgress> progress, CancellationToken ct = default);

    /// <summary>Upserts every row progress entry in <paramref name="progress" /> in a single transaction.</summary>
    Task UpsertRowsAsync(IReadOnlyList<RowProgress> progress, CancellationToken ct = default);

    /// <summary>Gets the last known aggregate phase for <paramref name="requestId" />, or <see langword="null" /> if none recorded.</summary>
    Task<RequestProgress?> GetRequestAsync(string requestId, CancellationToken ct = default);

    /// <summary>Gets every job progress entry recorded for <paramref name="requestId" />, keyed by job id.</summary>
    Task<IReadOnlyDictionary<string, JobProgress>> GetJobsAsync(string requestId, CancellationToken ct = default);

    /// <summary>Gets every row progress entry recorded for <paramref name="jobId" />, keyed by row index.</summary>
    Task<IReadOnlyDictionary<int, RowProgress>> GetRowsAsync(string requestId, string jobId, CancellationToken ct = default);

    /// <summary>Deletes every Request/Job/Row row recorded for <paramref name="requestId" />.</summary>
    Task DeleteRequestAsync(string requestId, CancellationToken ct = default);
}

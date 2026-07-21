/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: CacheEvictionRules.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Application.Rules;

/// <summary>
///     Pure eviction policy for the shared URL-resolution/download cache: rows older than
///     <see cref="Ttl" /> are outdated, and the database is trimmed by <see cref="EvictionRatio" />
///     whenever it exceeds <see cref="MaxSizeBytes" />.
/// </summary>
public static class CacheEvictionRules
{
    /// <summary>Maximum age of a cache row before it is considered outdated.</summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromDays(14);

    /// <summary>Maximum size of the cache database before eviction kicks in.</summary>
    public const long MaxSizeBytes = 50L * 1024 * 1024; // 50MB

    /// <summary>Fraction of rows (oldest first) evicted per table when over budget.</summary>
    public const double EvictionRatio = 0.20; // 20% db

    /// <summary>Returns whether <paramref name="timestamp" /> is older than <see cref="Ttl" /> relative to <paramref name="now" />.</summary>
    public static bool IsExpired(DateTimeOffset timestamp, DateTimeOffset now) => now - timestamp >= Ttl;

    /// <summary>Returns the cutoff timestamp: rows older than this are expired.</summary>
    public static DateTimeOffset ExpiryCutoff(DateTimeOffset now) => now - Ttl;

    /// <summary>Returns whether <paramref name="currentSizeBytes" /> exceeds <see cref="MaxSizeBytes" />.</summary>
    public static bool IsOverBudget(long currentSizeBytes) => currentSizeBytes >= MaxSizeBytes;

    /// <summary>Returns how many rows (oldest first) to evict from a table with <paramref name="totalRowCount" /> rows.</summary>
    public static int RowsToEvict(long totalRowCount) => (int)(totalRowCount * EvictionRatio);
}
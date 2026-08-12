/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ICache.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Cloud;
using SlideGenerator.Settings.Rules;
using SlideGenerator.Utilities;

namespace SlideGenerator.Generator.Abstractions;

/// <summary>
///     Shared, app-wide cache (backed by SQLite), split into two independent parts:
///     <see cref="InspectedUrls" /> (URL inspection results) and <see cref="DownloadedFiles" />
///     (the permanent temp-cache file registry).
///     Public only because <see cref="Steps.InspectUrlsStep" />'s primary constructor (resolved by
///     WorkflowCore's DI container, which requires public constructors) takes it as a parameter;
///     not intended for consumption outside this module.
/// </summary>
public interface ICache
{
    /// <summary>URL inspection result cache (source string → resolved <c>ContentInfo</c>).</summary>
    IInspectedUrlCache InspectedUrls { get; }

    /// <summary>Permanent temp-cache file registry (resolved URI → extension).</summary>
    IDownloadedFileCache DownloadedFiles { get; }
}

/// <summary>
///     Shared, app-wide cache of URL inspection results (<see cref="ICache.InspectedUrls" />).
/// </summary>
public interface IInspectedUrlCache
{
    /// <summary>
    ///     Batched lookup: collects every distinct <paramref name="sources" /> up front and resolves the
    ///     whole set with a single SELECT, invoking <paramref name="factory" /> only for the misses (no
    ///     row at all — a cached row with a NULL value is still a hit and resolves to <see langword="null" />
    ///     without invoking the factory again) and writing all cacheable misses back in one transaction.
    ///     Caching the whole <see cref="ContentInfo" /> (not just the resolved URI) means a cache hit
    ///     still carries <see cref="ContentInfo.Extension" /> without re-inspecting.
    /// </summary>
    /// <param name="sources">Distinct original source strings (URL or local path) used as cache keys.</param>
    /// <param name="factory">
    ///     Computes the value for one source on a miss. Returns <c>Cacheable=false</c> when the outcome is
    ///     not certain (e.g., the inspected request failed entirely due to a network error) — such results
    ///     are never written to the cache, even when <c>Value</c> is <see langword="null" />.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     Result keyed by the original source string. A non-cacheable miss (transient failure) is
    ///     <b>omitted</b> from the result entirely — distinct from an explicit <see langword="null" />
    ///     entry, which means the source was definitively resolved as not-an-image. Callers relying on
    ///     "already resolved" as a skip condition (e.g., across step re-runs) must key off dictionary
    ///     presence, not just a non-null value, so a failed source is retried instead of treated as done.
    /// </returns>
    Task<IReadOnlyDictionary<string, ContentInfo?>> GetOrUpdateManyAsync(
        IReadOnlyCollection<string> sources,
        Func<string, CancellationToken, Task<(bool Cacheable, ContentInfo? Value)>> factory,
        CancellationToken ct);
}

/// <summary>
///     Shared, app-wide registry (<see cref="ICache.DownloadedFiles" />) of resolved URIs whose content
///     is (or should be) materialized at a permanent temp-cache file. No consumer count is tracked —
///     unlike a reference-counted cache, an entry is not deleted when the last consumer is "done" with
///     it; it is kept until it naturally expires via TTL eviction, so later jobs/rows referencing the
///     same resolved URI can reuse the file directly instead of re-downloading it.
/// </summary>
public interface IDownloadedFileCache
{
    /// <summary>
    ///     Marks each of <paramref name="entries" /> as relevant to the permanent temp-cache in a single
    ///     batched writer, resetting its TTL clock (an upsert, not an increment) — call this whenever a
    ///     resolved URI is inspected/used, whether its file already exists on disk.
    /// </summary>
    /// <param name="entries">Resolved URI and file extension for each entry to touch.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TouchManyAsync(IReadOnlyCollection<(Uri ResolvedUri, string? Extension)> entries, CancellationToken ct);

    /// <summary>
    ///     Returns the full path of the temp-cached download file for <paramref name="resolvedUri" />,
    ///     named after its hash under <see cref="NameAndPaths.TempFolder" />.
    /// </summary>
    /// <param name="resolvedUri">The resolved URI to compute the cache filename from.</param>
    /// <param name="extension">File extension including the leading dot, or <see langword="null" />.</param>
    static string GetPath(Uri resolvedUri, string? extension) =>
        Path.Combine(NameAndPaths.TempFolder.RootPath, $"{resolvedUri.AbsoluteUri.HashText()}{extension}");
}

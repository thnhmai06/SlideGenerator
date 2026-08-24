/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ISummaryCache.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Summarizer;
using SlideGenerator.Summarizer.Presentations;
using SlideGenerator.Summarizer.Workbooks;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Services;

/// <summary>
///     Caches <see cref="ISummarizationService" /> results in memory, keyed by (path, last-write-time,
///     preview flag) — the Recipe editor re-summarizes the same workbook/presentation on almost every render
///     (canvas redraw, source list refresh, …), and each call opens the file via Syncfusion. A stale entry is
///     evicted automatically the moment the file's last-write-time changes, so this never serves outdated data.
/// </summary>
public interface ISummaryCache
{
    /// <inheritdoc cref="ISummarizationService.SummarizeWorkbookAsync" />
    Task<WorkbookSummary> GetWorkbookAsync(WorkbookIdentifier identifier, bool getPreview = true, CancellationToken ct = default);

    /// <inheritdoc cref="ISummarizationService.SummarizePresentationAsync" />
    Task<PresentationSummary> GetPresentationAsync(PresentationIdentifier identifier, bool getPreview = true, CancellationToken ct = default);
}

/// <inheritdoc cref="ISummaryCache" />
public sealed class SummaryCache(ISummarizationService inner) : ISummaryCache
{
    private readonly ConcurrentDictionary<(string Path, DateTime LastWriteUtc, bool GetPreview), Task<WorkbookSummary>> _workbooks = new();
    private readonly ConcurrentDictionary<(string Path, DateTime LastWriteUtc, bool GetPreview), Task<PresentationSummary>> _presentations = new();

    /// <inheritdoc />
    public Task<WorkbookSummary> GetWorkbookAsync(WorkbookIdentifier identifier, bool getPreview = true, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = (identifier.BookPath, File.GetLastWriteTimeUtc(identifier.BookPath), getPreview);
        return GetOrAddAsync(_workbooks, key, () => inner.SummarizeWorkbookAsync(identifier, getPreview, CancellationToken.None));
    }

    /// <inheritdoc />
    public Task<PresentationSummary> GetPresentationAsync(PresentationIdentifier identifier, bool getPreview = true, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = (identifier.PresentationPath, File.GetLastWriteTimeUtc(identifier.PresentationPath), getPreview);
        return GetOrAddAsync(_presentations, key, () => inner.SummarizePresentationAsync(identifier, getPreview, CancellationToken.None));
    }

    // The cached Task is shared across every caller for the same key, so it must not carry any one caller's
    // CancellationToken (cancelling caller A's request would fault the result caller B is awaiting) — the
    // factory above always passes CancellationToken.None to the inner service. A faulted result (e.g. the
    // workbook was open in Excel and locked) must not poison the key forever, since the file's last-write-time
    // won't change just because the lock clears — evict on failure so the next call retries.
    private static async Task<T> GetOrAddAsync<TKey, T>(ConcurrentDictionary<TKey, Task<T>> cache, TKey key, Func<Task<T>> factory)
        where TKey : notnull
    {
        var task = cache.GetOrAdd(key, _ => factory());
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            cache.TryRemove(new KeyValuePair<TKey, Task<T>>(key, task));
            throw;
        }
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Utilities.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Cloud.Models;
using SlideGenerator.Document.Abstractions.Sheet;
using SlideGenerator.Document.Models.Sheet;
using SlideGenerator.Settings.Abstractions;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Generator;

internal static class Utilities
{
    /// <summary>Case-insensitive equality on <see cref="ColumnIdentifier.ColumnName" />.</summary>
    private sealed class ColumnIdentifierComparer : IEqualityComparer<ColumnIdentifier>
    {
        public static readonly ColumnIdentifierComparer Instance = new();

        public bool Equals(ColumnIdentifier? x, ColumnIdentifier? y) =>
            string.Equals(x?.ColumnName, y?.ColumnName, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(ColumnIdentifier obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ColumnName);
    }

    /// <summary>
    ///     Creates a fresh <see cref="HttpClient" /> from <paramref name="httpClientFactory" /> (cheap — the
    ///     underlying <see cref="HttpMessageHandler" /> is pooled/reused by the factory) configured with the
    ///     current <c>Network</c> setting's timeout. Proxy is applied on the pooled handler itself (see
    ///     <c>AddHttpClient</c> registration in <c>Registration.cs</c>), refreshed whenever the factory
    ///     recycles the handler. Call this at the point of use instead of caching one client for reuse.
    /// </summary>
    internal static HttpClient CreateHttpClientWithSetting(this IHttpClientFactory httpClientFactory,
        ISettingProvider settingProvider)
    {
        var client = httpClientFactory.CreateClient(NameAndPaths.Application.Name);
        client.Timeout = TimeSpan.FromSeconds(settingProvider.Current.Network.Retry.Timeout);
        return client;
    }

    /// <summary>
    ///     Runs <paramref name="action" />, retrying up to <paramref name="maxRetries" /> times on
    ///     failure (an exception, or a null result), waiting between attempts via
    ///     <see cref="SlideGenerator.Utilities.Math.ComputeBackoffDelay" /> (or <paramref name="delay" />
    ///     if supplied, for tests). A genuine cancellation via <paramref name="ct" /> is never retried —
    ///     it propagates immediately, and an exception on the final attempt also propagates (never swallowed).
    /// </summary>
    internal static async Task<T?> ExecuteWithBackoffAsync<T>(
        int maxRetries,
        TimeSpan maxRetryDelay,
        Func<Task<T>> action,
        CancellationToken ct,
        Func<int, CancellationToken, Task>? delay = null) where T : class?
    {
        delay ??= (attempt, innerCt) =>
            Task.Delay(SlideGenerator.Utilities.Math.ComputeBackoffDelay(attempt, maxRetryDelay), innerCt);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            T? result;
            try
            {
                result = await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // real cancellation/shutdown — never retried
            }
            catch (Exception) when (attempt < maxRetries)
            {
                result = null; // treated as a failed attempt below
            }

            if (result != null || attempt == maxRetries) return result;

            await delay(attempt, ct).ConfigureAwait(false);
        }

        return null;
    }

    /// <summary>Whether <paramref name="inspected" />'s known content length exceeds <paramref name="maxDownloadBytes" /> (0 = unlimited).</summary>
    internal static bool ExceedsMaxDownloadBytes(ContentInfo? inspected, uint maxDownloadBytes) =>
        maxDownloadBytes > 0 && inspected?.Length is { } length && length > maxDownloadBytes;

    /// <summary>
    ///     Returns the trimmed value of the first non-empty column in <paramref name="columns" />,
    ///     or <see cref="string.Empty" /> if none of them have a value for this row.
    /// </summary>
    internal static string GetSource(IReadOnlyDictionary<ColumnIdentifier, string> rowValues,
        IReadOnlyList<ColumnIdentifier> columns)
    {
        foreach (var col in columns)
        {
            var val = rowValues.GetValueOrDefault(col);
            if (string.IsNullOrWhiteSpace(val)) continue;
            return val.Trim();
        }

        return string.Empty;
    }

    /// <summary>
    ///     Reads the header row (row 1) and returns a case-insensitive map of the column identifier → 0-based
    ///     list index. The index matches the position returned by <see cref="IReadOnlyWorksheet.GetRow" />, so
    ///     callers can do <c>row[headerMap[colId]]</c> directly.
    /// </summary>
    /// <param name="worksheet">Worksheet to read the header from.</param>
    /// <returns>Dictionary keyed by <see cref="ColumnIdentifier" /> (ordinal-ignore-case), value is a 0-based list index.</returns>
    internal static IReadOnlyDictionary<ColumnIdentifier, int> BuildHeaderToIndexMap(IReadOnlyWorksheet worksheet)
    {
        var headerRow = worksheet.GetRow(1);
        var result = new Dictionary<ColumnIdentifier, int>(ColumnIdentifierComparer.Instance);
        for (var i = 0; i < headerRow.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(headerRow[i])) continue;
            var columnId = new ColumnIdentifier(headerRow[i]);
            result.TryAdd(columnId, i);
        }

        return result;
    }
}
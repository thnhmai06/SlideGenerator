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

using SlideGenerator.Document.Workbooks.Components;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Settings.Mutable;
using Math = SlideGenerator.Utilities.Math;

namespace SlideGenerator.Generator;

internal static class Utilities
{
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
            Task.Delay(Math.ComputeBackoffDelay(attempt, maxRetryDelay), innerCt);

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

    /// <summary>
    ///     Restricts <paramref name="headerToIndex" /> to the columns named in <paramref name="usedColumns" />
    ///     (the column headers visible to the job's instructions). When <paramref name="usedColumns" /> is
    ///     <see langword="null" /> every column is used and the map is returned as-is.
    /// </summary>
    /// <param name="headerToIndex">The worksheet's full column map, as built by <see cref="BuildHeaderToIndexMap" />.</param>
    /// <param name="usedColumns">The columns to keep, or <see langword="null" /> to keep them all.</param>
    /// <returns>
    ///     A case-insensitive map holding only the used columns (with their original indexes), or
    ///     <paramref name="headerToIndex" /> itself when <paramref name="usedColumns" /> is <see langword="null" />.
    /// </returns>
    internal static IReadOnlyDictionary<ColumnIdentifier, int> FilterColumns(
        IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex, IReadOnlySet<ColumnIdentifier>? usedColumns)
    {
        if (usedColumns is null) return headerToIndex;

        var usedNames = new HashSet<string>(usedColumns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<ColumnIdentifier, int>(ColumnIdentifierComparer.Instance);
        foreach (var (column, index) in headerToIndex)
            if (usedNames.Contains(column.ColumnName))
                result[column] = index;

        return result;
    }

    /// <summary>
    ///     Reads the requested data rows, calling <see cref="IReadOnlyWorksheet.GetRow" /> exactly once per row.
    ///     Element <c>i</c> of the result is the value map for <c>dataRows[i]</c>; each map is keyed by the columns
    ///     of <paramref name="headerToIndex" /> (ordinal-ignore-case) and missing cells read as
    ///     <see cref="string.Empty" />.
    /// </summary>
    /// <param name="worksheet">Worksheet to read from.</param>
    /// <param name="headerToIndex">Column → 0-based list index map, already filtered to the used columns.</param>
    /// <param name="dataRows">
    ///     1-based data row indices; the worksheet row is <c>dataRow + 1</c> (row 1 = the header row).
    /// </param>
    /// <returns>One value map per entry of <paramref name="dataRows" />, in the same order.</returns>
    internal static IReadOnlyList<IReadOnlyDictionary<ColumnIdentifier, string>> ReadRows(
        IReadOnlyWorksheet worksheet, IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex,
        IReadOnlyList<int> dataRows)
    {
        var result = new List<IReadOnlyDictionary<ColumnIdentifier, string>>(dataRows.Count);
        foreach (var dataRow in dataRows)
        {
            var row = worksheet.GetRow(dataRow + 1); // row 1 = header => data row N = absolute row N+1
            var values = new Dictionary<ColumnIdentifier, string>(
                headerToIndex.Count, ColumnIdentifierComparer.Instance);
            foreach (var (column, index) in headerToIndex)
                values[column] = index < row.Count ? row[index] : string.Empty;
            result.Add(values);
        }

        return result;
    }

    /// <summary>Case-insensitive equality on <see cref="ColumnIdentifier.ColumnName" />.</summary>
    private sealed class ColumnIdentifierComparer : IEqualityComparer<ColumnIdentifier>
    {
        public static readonly ColumnIdentifierComparer Instance = new();

        public bool Equals(ColumnIdentifier? x, ColumnIdentifier? y)
        {
            return string.Equals(x?.ColumnName, y?.ColumnName, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ColumnIdentifier obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ColumnName);
        }
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: LogFileReader.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using System.Text.RegularExpressions;

namespace SlideGenerator.Generator.Progress;

/// <summary>
///     Reads and parses a workflow log file back into scoped <see cref="LogEntry" /> records, for serving
///     <c>Summary.Logs</c>/<c>JobSummary.Logs</c>/<c>RowSummary.Logs</c> without a dedicated log database
///     table. Reads straight from disk on every call — no in-memory cache — since <c>Summary</c> is polled
///     infrequently.
/// </summary>
public interface ILogFileReader
{
    /// <summary>
    ///     Reads every parseable line of <paramref name="logPath" /> into a <see cref="LogEntry" />. Lines
    ///     that don't match the expected <c>FileLogFormatter</c> shape (e.g. exception stack trace
    ///     continuation lines) are skipped. Returns an empty list if the file doesn't exist.
    /// </summary>
    IReadOnlyList<LogEntry> ReadAll(string logPath);
}

/// <summary>
///     Reads a workflow log file written by <c>FileLogFormatter</c> and parses each line back into a
///     <see cref="LogEntry" />. Line shape: <c>[{timestamp}] [{loggerName}/{path}] {levelAbbr}: {message}</c>.
/// </summary>
internal sealed partial class LogFileReader : ILogFileReader
{
    [GeneratedRegex(@"^\[(?<ts>[^\]]+)\] \[(?<logger>[^/\]]*)/(?<path>[^\]]*)\] (?<level>\w{3}): (?<msg>.*)$")]
    private static partial Regex LinePattern();

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> ReadAll(string logPath)
    {
        if (!File.Exists(logPath)) return [];

        var entries = new List<LogEntry>();
        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var match = LinePattern().Match(line);
            if (!match.Success) continue;

            if (!DateTimeOffset.TryParseExact(match.Groups["ts"].Value, "yyyy-MM-dd HH:mm:ss.fff zzz",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                continue;

            entries.Add(new LogEntry
            {
                Timestamp = timestamp,
                Path = match.Groups["path"].Value,
                Level = match.Groups["level"].Value,
                Info = match.Groups["msg"].Value
            });
        }

        return entries;
    }
}

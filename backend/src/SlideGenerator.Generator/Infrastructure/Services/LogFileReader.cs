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
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Data;

namespace SlideGenerator.Generator.Infrastructure.Services;

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

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ILogFileReader.cs
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

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: IFileLoggerFactory.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;

namespace SlideGenerator.Logging.Abstractions;

/// <summary>
///     Creates file-backed <see cref="ILoggerFactory" /> instances for isolated log files.
/// </summary>
public interface IFileLoggerFactory
{
    /// <summary>
    ///     Creates an <see cref="ILoggerFactory" /> that writes all log events to a specific file.
    /// </summary>
    /// <param name="filePath">The file path to write log events to.</param>
    /// <param name="scope">
    ///     Optional static scope label enriched onto every log event written by this factory
    ///     (e.g. <c>"Workflow/abc123"</c>). Appears as the <c>Scope</c> property in the formatter.
    /// </param>
    /// <returns>A configured <see cref="ILoggerFactory" /> backed by the specified file.</returns>
    ILoggerFactory CreateFile(string filePath, string? scope = null);
}
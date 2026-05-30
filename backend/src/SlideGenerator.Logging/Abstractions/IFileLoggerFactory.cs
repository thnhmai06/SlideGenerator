/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: IFileLoggerFactory.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
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
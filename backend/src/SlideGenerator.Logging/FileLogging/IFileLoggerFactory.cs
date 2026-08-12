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

namespace SlideGenerator.Logging.FileLogging;

/// <summary>
///     Creates file-backed <see cref="ILoggerFactory" /> instances for isolated log files.
/// </summary>
public interface IFileLoggerFactory
{
    /// <summary>
    ///     Creates an <see cref="ILoggerFactory" /> that writes all log events to a specific file.
    /// </summary>
    /// <param name="filePath">The file path to write log events to.</param>
    /// <param name="scopePropertyNames">
    ///     Ordered ambient property names (pushed via <c>Serilog.Context.LogContext.PushProperty</c>) to join
    ///     into each event's scope path — e.g. <c>["RequestId", "JobId", "RowIndex"]</c>. This module has no
    ///     notion of what a scope means; the caller owns the property names. <see langword="null" /> or empty
    ///     yields an empty scope path for every event.
    /// </param>
    /// <param name="onLogEvent">
    ///     Optional callback invoked for every log event written through this factory, carrying its scope
    ///     path. Used to forward log lines to the frontend in real time without adding a second persistence
    ///     store.
    /// </param>
    /// <returns>A configured <see cref="ILoggerFactory" /> backed by the specified file.</returns>
    ILoggerFactory CreateFile(
        string filePath, IReadOnlyList<string>? scopePropertyNames = null, Action<LogNotification>? onLogEvent = null);
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SerilogFileLoggerFactory.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace SlideGenerator.Logging.FileLogging;

/// <summary>
///     Creates file-backed <see cref="ILoggerFactory" /> instances using the Serilog infrastructure.
///     Each factory owns a dedicated Serilog file sink; callers obtain named
///     <see cref="Microsoft.Extensions.Logging.ILogger" />
///     instances via <see cref="ILoggerFactory.CreateLogger" />.
/// </summary>
internal sealed class SerilogFileLoggerFactory(LoggerConfiguration config) : IFileLoggerFactory
{
    /// <inheritdoc />
    public ILoggerFactory CreateFile(
        string filePath, IReadOnlyList<string>? scopePropertyNames = null, Action<LogNotification>? onLogEvent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var scope = scopePropertyNames ?? [];
        var loggerConfig = config.WriteTo.File(new FileLogFormatter(scope), filePath);
        if (onLogEvent != null)
            loggerConfig = loggerConfig.WriteTo.Sink(new ScopeNotifyingSink(scope, onLogEvent));

        return new SerilogLoggerFactory(loggerConfig.CreateLogger(), true);
    }
}
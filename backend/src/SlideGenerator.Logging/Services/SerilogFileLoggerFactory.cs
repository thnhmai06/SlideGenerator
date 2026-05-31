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
using SlideGenerator.Logging.Abstractions;
using SlideGenerator.Logging.Formats;

namespace SlideGenerator.Logging.Services;

/// <summary>
///     Creates file-backed <see cref="ILoggerFactory" /> instances using the Serilog infrastructure.
///     Each factory owns a dedicated Serilog file sink; callers obtain named
///     <see cref="Microsoft.Extensions.Logging.ILogger" />
///     instances via <see cref="ILoggerFactory.CreateLogger" />.
/// </summary>
internal sealed class SerilogFileLoggerFactory(LoggerConfiguration config) : IFileLoggerFactory
{
    /// <inheritdoc />
    public ILoggerFactory CreateFile(string filePath, string? scope = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var localConfig = config;
        if (scope != null)
            localConfig = localConfig.Enrich.WithProperty("Scope", scope);

        var serilogLogger = localConfig
            .WriteTo.File(new FileLogFormatter(), filePath)
            .CreateLogger();

        return new SerilogLoggerFactory(serilogLogger, true);
    }
}
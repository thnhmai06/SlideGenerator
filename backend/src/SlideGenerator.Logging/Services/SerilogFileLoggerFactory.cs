/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SerilogFileLoggerFactory.cs
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
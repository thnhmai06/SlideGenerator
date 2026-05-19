/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: SerilogAppLogger.cs
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

using Serilog;
using Serilog.Context;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Adapts a Serilog logger to the Clean Architecture logging abstraction.
/// </summary>
/// <param name="logger">The Serilog logger instance that writes events to configured sinks.</param>
/// <param name="scopeManager">The scope manager that tracks hierarchical business scopes.</param>
internal sealed class SerilogAppLogger(ILogger logger, IScopeManager scopeManager) : ISystemLogger
{
    /// <inheritdoc />
    public IDisposable BeginScope(string scope)
    {
        var scopeHandle = scopeManager.BeginScope(scope);
        var propertyHandle = LogContext.PushProperty("Scope", scopeManager.CurrentScope);
        return new CompositeDisposable(propertyHandle, scopeHandle);
    }

    /// <inheritdoc />
    public void Trace(string messageTemplate, params object?[] propertyValues)
    {
        logger.Verbose(messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Debug(string messageTemplate, params object?[] propertyValues)
    {
        logger.Debug(messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Information(string messageTemplate, params object?[] propertyValues)
    {
        logger.Information(messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Warning(string messageTemplate, params object?[] propertyValues)
    {
        logger.Warning(messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Warning(Exception exception, string messageTemplate, params object?[] propertyValues)
    {
        logger.Warning(exception, messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Error(string messageTemplate, params object?[] propertyValues)
    {
        logger.Error(messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Error(Exception exception, string messageTemplate, params object?[] propertyValues)
    {
        logger.Error(exception, messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Fatal(string messageTemplate, params object?[] propertyValues)
    {
        logger.Fatal(messageTemplate, propertyValues);
    }

    /// <inheritdoc />
    public void Fatal(Exception exception, string messageTemplate, params object?[] propertyValues)
    {
        logger.Fatal(exception, messageTemplate, propertyValues);
    }
}
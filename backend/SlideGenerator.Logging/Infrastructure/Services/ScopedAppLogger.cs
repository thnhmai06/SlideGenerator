/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: ScopedAppLogger.cs
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
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Logging.Infrastructure.Services;

/// <summary>
///     Decorates an application logger so each write is emitted inside a fixed root scope.
/// </summary>
/// <param name="inner">The logger that performs the actual write.</param>
/// <param name="rootScope">The scope prefix applied to every log event.</param>
internal sealed class ScopedAppLogger(IAppLogger inner, string rootScope) : IAppLogger
{
    /// <inheritdoc />
    public IDisposable BeginScope(string scope)
    {
        return inner.BeginScope($"{rootScope}/{scope}");
    }

    /// <inheritdoc />
    public void Trace(string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Trace(messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Debug(string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Debug(messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Information(string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Information(messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Warning(string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Warning(messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Warning(Exception exception, string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Warning(exception, messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Error(string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Error(messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Error(Exception exception, string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Error(exception, messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Fatal(string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Fatal(messageTemplate, propertyValues);
        }
    }

    /// <inheritdoc />
    public void Fatal(Exception exception, string messageTemplate, params object?[] propertyValues)
    {
        using (inner.BeginScope(rootScope))
        {
            inner.Fatal(exception, messageTemplate, propertyValues);
        }
    }
}

/// <summary>
///     Provides infrastructure-only helpers for decorating application loggers.
/// </summary>
internal static class AppLoggerScopeExtensions
{
    /// <summary>
    ///     Wraps a logger so each event is written under the supplied root scope.
    /// </summary>
    /// <param name="logger">The logger to decorate.</param>
    /// <param name="rootScope">The scope prefix applied to all writes.</param>
    /// <returns>A logger decorated with the specified root scope.</returns>
    public static IAppLogger WithScope(this IAppLogger logger, string rootScope)
    {
        return new ScopedAppLogger(logger, rootScope);
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: IAppLogger.cs
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

namespace SlideGenerator.Logging.Domain.Abstractions;

/// <summary>
///     Defines application logging operations.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    ///     Begins a hierarchical logging scope for the current execution context.
    /// </summary>
    /// <param name="scope">The scope path, for example <c>Auth/Login</c>.</param>
    /// <returns>A disposable handle that restores the previous scope when disposed.</returns>
    IDisposable BeginScope(string scope);

    /// <summary>
    ///     Writes a trace log event.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Trace(string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes a debug log event.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Debug(string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes an information log event.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Information(string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes a warning log event.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Warning(string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes a warning log event with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Warning(Exception exception, string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes an error log event.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Error(string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes an error log event with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Error(Exception exception, string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes a fatal log event.
    /// </summary>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Fatal(string messageTemplate, params object?[] propertyValues);

    /// <summary>
    ///     Writes a fatal log event with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="propertyValues">The message property values.</param>
    void Fatal(Exception exception, string messageTemplate, params object?[] propertyValues);
}
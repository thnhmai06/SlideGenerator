/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: ScopedExceptionFormatter.cs
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
using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace SlideGenerator.Logging.Infrastructure.Formatting;

/// <summary>
///     Formats log events as scoped single-line messages and optional exception-detail JSON.
/// </summary>
/// <remarks>
///     The first line is always formatted as <c>[Timestamp] [Level] [Scope] [Message]</c>.
///     For <see cref="LogEventLevel.Error" /> and <see cref="LogEventLevel.Fatal" /> events that contain an exception,
///     the formatter writes one additional JSON line using the <c>ExceptionDetail</c> property enriched by
///     Serilog.Exceptions when available.
/// </remarks>
public sealed class ScopedExceptionFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _jsonValueFormatter = new("$type");

    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        var level = logEvent.Level.ToString();
        var scope = GetScalarValue(logEvent, "Scope") ?? "Global";
        var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

        output.WriteLine($"[{timestamp}] [{level}] [{scope}] [{message}]");

        if (logEvent.Exception is null || logEvent.Level < LogEventLevel.Error) return;

        output.WriteLine(CreateExceptionJson(logEvent));
    }

    /// <summary>
    ///     Creates the JSON line that represents exception details for an error or fatal event.
    /// </summary>
    /// <param name="logEvent">The Serilog event that contains the exception.</param>
    /// <returns>A single-line JSON representation of the exception details.</returns>
    private string CreateExceptionJson(LogEvent logEvent)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        if (logEvent.Properties.TryGetValue("ExceptionDetail", out var exceptionDetail))
        {
            _jsonValueFormatter.Format(exceptionDetail, writer);
            return writer.ToString();
        }

        var ex = logEvent.Exception!;
        writer.Write(
            $"{{\"Type\":\"{JsonEncodedString(ex.GetType().FullName ?? ex.GetType().Name)}\",\"Message\":\"{JsonEncodedString(ex.Message)}\",\"StackTrace\":\"{JsonEncodedString(ex.StackTrace ?? string.Empty)}\"}}");
        return writer.ToString();
    }

    /// <summary>
    ///     Reads a scalar Serilog property value as text.
    /// </summary>
    /// <param name="logEvent">The event containing structured properties.</param>
    /// <param name="propertyName">The name of the scalar property to read.</param>
    /// <returns>The scalar value as text, or <see langword="null" /> when it is missing or not scalar.</returns>
    private static string? GetScalarValue(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value) &&
               value is ScalarValue { Value: not null } scalar
            ? scalar.Value.ToString()
            : null;
    }

    /// <summary>
    ///     Escapes a string value for safe insertion into a minimal JSON string literal.
    /// </summary>
    /// <param name="value">The unescaped value.</param>
    /// <returns>The escaped JSON string content without surrounding quotes.</returns>
    private static string JsonEncodedString(string value)
    {
        return value
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}


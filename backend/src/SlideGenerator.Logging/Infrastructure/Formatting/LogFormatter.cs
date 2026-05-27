/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: LogFormatter.cs
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

namespace SlideGenerator.Logging.Infrastructure.Formatting;

/// <summary>
///     Formats log events as structured single-line messages with optional human-readable exception detail.
/// </summary>
/// <remarks>
///     The first line is always formatted as <c>[Timestamp] [LoggerName/Scope] LVL: Message</c>.
///     For <see cref="LogEventLevel.Warning" /> events that contain an exception, one summary line is appended.
///     For <see cref="LogEventLevel.Error" /> and <see cref="LogEventLevel.Fatal" /> events, the full exception
///     chain with stack trace is written in a human-readable indented format.
/// </remarks>
internal sealed class LogFormatter : ITextFormatter
{
    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        var levelAbbr = logEvent.Level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => "???"
        };

        var loggerName = GetScalarValue(logEvent, "LoggerName") ?? "?";
        var scope = GetScalarValue(logEvent, "Scope") ?? "Global";
        var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

        output.WriteLine($"[{timestamp}] [{loggerName}/{scope}] {levelAbbr}: {message}");

        if (logEvent.Exception is null || logEvent.Level < LogEventLevel.Warning) return;

        if (logEvent.Level < LogEventLevel.Error)
        {
            output.WriteLine($"  ! {logEvent.Exception.GetType().Name}: {logEvent.Exception.Message}");
            return;
        }

        WriteExceptionDetail(logEvent.Exception, output);
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
    ///     Writes a human-readable exception chain with indented stack traces to <paramref name="output" />.
    /// </summary>
    private static void WriteExceptionDetail(Exception ex, TextWriter output, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        output.WriteLine($"{indent}  ! {ex.GetType().FullName}: {ex.Message}");

        if (!string.IsNullOrEmpty(ex.StackTrace))
            output.WriteLine(
                $"{indent}    {ex.StackTrace.Replace(Environment.NewLine, $"{Environment.NewLine}{indent}    ", StringComparison.Ordinal)}");

        if (ex.InnerException is not null)
        {
            output.WriteLine($"{indent}  Caused by:");
            WriteExceptionDetail(ex.InnerException, output, depth + 1);
        }
    }
}
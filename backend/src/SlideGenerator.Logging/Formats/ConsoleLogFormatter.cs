/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: ConsoleLogFormatter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;

namespace SlideGenerator.Logging.Formats;

/// <summary>
///     Formats log events for console output as compact single-line messages.
/// </summary>
/// <remarks>
///     The first line is always formatted as <c>[Timestamp] [LoggerName/Scope] LVL: Message</c>.
///     For <see cref="LogEventLevel.Warning" /> and above events that contain an exception,
///     a compact one-line summary (<c>ExceptionType: message</c>) is appended.
///     Stack traces are omitted to keep console output readable.
/// </remarks>
public sealed class ConsoleLogFormatter : ITextFormatter
{
    private const string Reset = "\e[0m";
    private const string Gray = "\e[37m";
    private const string DarkGray = "\e[90m";
    private const string Blue = "\e[34m";
    private const string Yellow = "\e[33m";
    private const string Red = "\e[31m";
    private const string Magenta = "\e[35m";

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
        var levelColor = logEvent.Level switch
        {
            LogEventLevel.Verbose => DarkGray,
            LogEventLevel.Debug => Gray,
            LogEventLevel.Information => Blue,
            LogEventLevel.Warning => Yellow,
            LogEventLevel.Error => Red,
            LogEventLevel.Fatal => Magenta,
            _ => Reset
        };

        var loggerName = logEvent.GetScalarValue("LoggerName")
                         ?? logEvent.GetScalarValue("SourceContext")
                         ?? "?";
        var scope = logEvent.GetScalarValue("Scope") ?? "Global";
        var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

        output.WriteLine($"[{timestamp}] [{loggerName}/{scope}] {levelColor}{levelAbbr}: {message}{Reset}");

        if (logEvent.Exception is null || logEvent.Level < LogEventLevel.Warning) return;

        var ex = logEvent.Exception;
        while (ex is not null)
        {
            output.WriteLine($"  {levelColor}!{Reset} {ex.GetType().Name}: {ex.Message}");
            ex = ex.InnerException;
        }
    }
}
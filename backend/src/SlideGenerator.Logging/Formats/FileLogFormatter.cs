/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: FileLogFormatter.cs
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
///     Formats log events for file output as structured single-line messages with human-readable exception detail.
/// </summary>
/// <remarks>
///     The first line is always formatted as <c>[Timestamp] [LoggerName/Scope] LVL: Message</c>.
///     For <see cref="LogEventLevel.Warning" /> events that contain an exception, one summary line is appended.
///     For <see cref="LogEventLevel.Error" /> and <see cref="LogEventLevel.Fatal" /> events, the full exception
///     chain with stack trace is written in a human-readable indented format.
/// </remarks>
public sealed class FileLogFormatter : ITextFormatter
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

        var loggerName = logEvent.GetScalarValue("LoggerName")
                         ?? logEvent.GetScalarValue("SourceContext")
                         ?? "?";
        var scope = logEvent.GetScalarValue("Scope") ?? "Global";
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
    ///     Writes a human-readable exception chain with indented stack traces to <paramref name="output" />.
    /// </summary>
    private static void WriteExceptionDetail(Exception ex, TextWriter output, int depth = 0)
    {
        while (true)
        {
            var indent = new string(' ', depth * 2);
            output.WriteLine($"{indent}  ! {ex.GetType().FullName}: {ex.Message}");

            if (!string.IsNullOrEmpty(ex.StackTrace))
                output.WriteLine($"{indent}\t{ex.StackTrace.Replace(
                    Environment.NewLine, $"{Environment.NewLine}{indent}\t", StringComparison.Ordinal)}");

            if (ex.InnerException is null) return;
            output.WriteLine($"{indent}  Caused by:");
            ex = ex.InnerException;
            depth++;
        }
    }
}
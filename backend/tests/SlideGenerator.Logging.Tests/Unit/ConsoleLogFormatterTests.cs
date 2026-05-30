/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging.Tests
 * File: ConsoleLogFormatterTests.cs
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

using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using SlideGenerator.Logging.Formats;
using Xunit;

namespace SlideGenerator.Logging.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="ConsoleLogFormatter" />.
/// </summary>
public sealed class ConsoleLogFormatterTests
{
    private const string Reset = "\e[0m";
    private static readonly MessageTemplateParser Parser = new();

    /// <summary>Information level and message are written in blue.</summary>
    [Fact]
    public void Format_Information_ColorsLevelAndMessage()
    {
        var output = Format(LogEventLevel.Information);

        output.Should().Contain($"\e[34mINF: Message{Reset}");
    }

    /// <summary>Warning exception marker uses the warning color.</summary>
    [Fact]
    public void Format_WarningWithException_ColorsExceptionMarker()
    {
        var output = Format(LogEventLevel.Warning, new InvalidOperationException("Invalid recipe"));

        output.Should().Contain($"\e[33mWRN: Message{Reset}");
        output.Should().Contain($"  \e[33m!{Reset} InvalidOperationException: Invalid recipe");
    }

    private static string Format(LogEventLevel level, Exception? exception = null)
    {
        var formatter = new ConsoleLogFormatter();
        using var output = new StringWriter();
        var logEvent = new LogEvent(
            DateTimeOffset.Parse("2026-05-30T10:15:30.000+07:00"),
            level,
            exception,
            Parser.Parse("Message"),
            [
                new LogEventProperty("LoggerName", new ScalarValue("Logger")),
                new LogEventProperty("Scope", new ScalarValue("Scope"))
            ]);

        formatter.Format(logEvent, output);

        return output.ToString();
    }
}

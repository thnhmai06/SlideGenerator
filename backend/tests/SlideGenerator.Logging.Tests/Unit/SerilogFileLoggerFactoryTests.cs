/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging.Tests
 * File: SerilogFileLoggerFactoryTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using SlideGenerator.Logging.FileLogging;
using Xunit;

namespace SlideGenerator.Logging.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SerilogFileLoggerFactory" />.
/// </summary>
public sealed class SerilogFileLoggerFactoryTests : IDisposable
{
    // Enrich.FromLogContext() mirrors the real DI-registered LoggerConfiguration (Program.Services.cs) —
    // without it, properties pushed via LogContext.PushProperty never reach the formatter/sink.
    private readonly SerilogFileLoggerFactory _factory = new(new LoggerConfiguration().Enrich.FromLogContext());
    private readonly List<string> _tempFiles = [];

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
            File.Delete(file);
    }

    private string NewTempFilePath()
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        return path;
    }

    /// <summary>Null file path throws ArgumentException.</summary>
    [Fact]
    public void CreateFile_NullPath_ThrowsArgumentException()
    {
        var act = () => _factory.CreateFile(null!);
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>Whitespace file path throws ArgumentException.</summary>
    [Fact]
    public void CreateFile_WhitespacePath_ThrowsArgumentException()
    {
        var act = () => _factory.CreateFile("   ");
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>Valid path returns a non-null ILoggerFactory that can create named loggers.</summary>
    [Fact]
    public void CreateFile_ValidPath_ReturnsNonNullLoggerFactory()
    {
        using var loggerFactory = _factory.CreateFile(NewTempFilePath());

        loggerFactory.Should().NotBeNull();
        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();
    }

    /// <summary><c>onLogEvent</c> callback parameter is accepted; the returned factory creates loggers without error.</summary>
    [Fact]
    public void CreateFile_WithOnLogEventCallback_LoggerFactoryCreatesNamedLogger()
    {
        using var loggerFactory = _factory.CreateFile(NewTempFilePath(), onLogEvent: _ => { });

        var logger = loggerFactory.CreateLogger(nameof(SerilogFileLoggerFactoryTests));
        logger.Should().NotBeNull();
    }

    #region Inner logger behaviour — mirrors real step usage (data.Transient.LoggerFactory.CreateLogger(nameof(Step)))

    /// <summary>Information log level is enabled by default — matches the minimum level of LoggerConfiguration.</summary>
    [Fact]
    public void CreateFile_ValidPath_InformationLevelIsEnabled()
    {
        using var loggerFactory = _factory.CreateFile(NewTempFilePath());
        var logger = loggerFactory.CreateLogger(nameof(SerilogFileLoggerFactoryTests));

        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    /// <summary>Log file is created on disk after the first log event is written.</summary>
    [Fact]
    public void CreateFile_ValidPath_FileIsCreatedAfterFirstWrite()
    {
        var filePath = NewTempFilePath();

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("Step").LogInformation("init");
        }

        File.Exists(filePath).Should().BeTrue();
    }

    /// <summary>
    ///     Logged message text appears in the file after dispose flushes the sink.
    ///     Mirrors the real step pattern: <c>data.Transient.LoggerFactory.CreateLogger(nameof(Step)).LogInformation(...)</c>.
    /// </summary>
    [Fact]
    public void CreateFile_ValidPath_LoggedMessageAppearsInFile()
    {
        var filePath = NewTempFilePath();
        const string expectedMessage = "Recipe loaded with 3 node(s)";

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("LoadRecipeSummary").LogInformation(expectedMessage);
        }

        File.ReadAllText(filePath).Should().Contain("LoadRecipeSummary").And.Contain(expectedMessage);
    }

    /// <summary>
    ///     The category name passed to <c>CreateLogger</c> appears in the file as the logger name field.
    ///     Real usage: <c>CreateLogger(nameof(LoadRecipeSummary))</c> → file shows <c>LoadRecipeSummary/...</c>
    /// </summary>
    [Fact]
    public void CreateFile_ValidPath_CategoryNameAppearsInFile()
    {
        var filePath = NewTempFilePath();
        const string categoryName = "LoadRecipeSummary";

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger(categoryName).LogInformation("step ran");
        }

        File.ReadAllText(filePath).Should().Contain(categoryName);
    }

    /// <summary>
    ///     When Request/Job scope properties are pushed onto <see cref="LogContext" /> around a log call,
    ///     the resulting scope path appears in the log line. Real usage: each Step pushes
    ///     <c>LogContext.PushProperty("RequestId", ...)</c>/<c>PushProperty("JobId", ...)</c> around its
    ///     <c>Run</c>/<c>RunAsync</c> body.
    /// </summary>
    [Fact]
    public void CreateFile_WithLogContextScope_ScopePathAppearsInFile()
    {
        var filePath = NewTempFilePath();

        using (var loggerFactory = _factory.CreateFile(filePath, ["RequestId", "JobId"]))
        using (LogContext.PushProperty("RequestId", "req-1"))
        using (LogContext.PushProperty("JobId", "job-1"))
        {
            loggerFactory.CreateLogger("AnyStep").LogInformation("step message");
        }

        File.ReadAllText(filePath).Should().Contain("AnyStep").And.Contain("req-1/job-1");
    }

    /// <summary>
    ///     The <c>onLogEvent</c> callback is invoked once per log line written through the factory, carrying
    ///     the rendered message and scope path — the mechanism <c>Middleware</c> uses to forward log lines
    ///     to the frontend in real time.
    /// </summary>
    [Fact]
    public void CreateFile_WithOnLogEventCallback_InvokedForEachLogLine()
    {
        var filePath = NewTempFilePath();
        var notifications = new List<LogNotification>();

        using (var loggerFactory = _factory.CreateFile(filePath, ["RequestId"], notifications.Add))
        using (LogContext.PushProperty("RequestId", "req-1"))
        {
            loggerFactory.CreateLogger("AnyStep").LogInformation("step message");
        }

        notifications.Should().ContainSingle(n => n.Message == "step message" && n.Location == "req-1");
    }

    /// <summary>
    ///     Multiple named loggers from the same factory all write to the same file.
    ///     Mirrors how each step calls <c>data.Transient.LoggerFactory.CreateLogger(nameof(Step))</c> independently.
    /// </summary>
    [Fact]
    public void CreateFile_ValidPath_MultipleNamedLoggers_AllMessagesInFile()
    {
        var filePath = NewTempFilePath();

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("LoadRecipeSummary").LogInformation("recipe loaded");
            loggerFactory.CreateLogger("PreflightCleanup").LogInformation("cleanup done");
            loggerFactory.CreateLogger("ValidateRequest").LogInformation("request valid");
        }

        var content = File.ReadAllText(filePath);
        content.Should().Contain("LoadRecipeSummary").And.Contain("recipe loaded");
        content.Should().Contain("PreflightCleanup").And.Contain("cleanup done");
        content.Should().Contain("ValidateRequest").And.Contain("request valid");
    }

    /// <summary>Exception message and type appear in the file for Error-level events.</summary>
    [Fact]
    public void CreateFile_ValidPath_LoggedExceptionAppearsInFile()
    {
        var filePath = NewTempFilePath();
        var exception = new InvalidOperationException("Recipe 42 not found");

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("LoadRecipeSummary")
                .LogError(exception, "Step failed");
        }

        var content = File.ReadAllText(filePath);
        content.Should().Contain("Recipe 42 not found");
        content.Should().Contain(nameof(InvalidOperationException));
    }

    /// <summary>Disposing the factory does not throw even when loggers have written events.</summary>
    [Fact]
    public void CreateFile_ValidPath_DisposeDoesNotThrow()
    {
        var loggerFactory = _factory.CreateFile(NewTempFilePath());
        loggerFactory.CreateLogger("Step").LogInformation("message");

        var act = loggerFactory.Dispose;

        act.Should().NotThrow();
    }

    #endregion
}
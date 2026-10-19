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
using Microsoft.Extensions.DependencyInjection;
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
    // without it, properties pushed via LogContext.PushProperty never reach the formatter/sink. The factory
    // takes a delegate, so every CreateFile call gets a fresh configuration.
    private readonly SerilogFileLoggerFactory _factory = new(() => new LoggerConfiguration().Enrich.FromLogContext());
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

    /// <summary>
    ///     A single factory instance can back several log files: each logger writes only to its own file, proving
    ///     the Serilog sinks do not accumulate on one shared configuration.
    /// </summary>
    [Fact]
    public void CreateFile_CalledTwiceOnSameInstance_EachLoggerWritesOnlyToItsOwnFile()
    {
        var firstPath = NewTempFilePath();
        var secondPath = NewTempFilePath();

        using (var first = _factory.CreateFile(firstPath))
        using (var second = _factory.CreateFile(secondPath))
        {
            first.CreateLogger(nameof(SerilogFileLoggerFactoryTests)).LogInformation("first file message");
            second.CreateLogger(nameof(SerilogFileLoggerFactoryTests)).LogInformation("second file message");
        }

        var firstContent = File.ReadAllText(firstPath);
        var secondContent = File.ReadAllText(secondPath);

        firstContent.Should().Contain("first file message").And.NotContain("second file message");
        secondContent.Should().Contain("second file message").And.NotContain("first file message");
    }

    /// <summary>
    ///     The factory resolved once from a container configured by <c>AddLoggingServices</c> — as the singleton
    ///     consumers hold it — can create two log files and both loggers write.
    /// </summary>
    [Fact]
    public void AddLoggingServices_FileLoggerFactory_CanCreateTwoFilesFromTheContainer()
    {
        var services = new ServiceCollection();
        services.AddTransient(_ => new LoggerConfiguration().Enrich.FromLogContext());
        services.AddLoggingServices();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IFileLoggerFactory>();

        var firstPath = NewTempFilePath();
        var secondPath = NewTempFilePath();

        using (var first = factory.CreateFile(firstPath))
        using (var second = factory.CreateFile(secondPath))
        {
            first.CreateLogger(nameof(SerilogFileLoggerFactoryTests)).LogInformation("container first message");
            second.CreateLogger(nameof(SerilogFileLoggerFactoryTests)).LogInformation("container second message");
        }

        File.ReadAllText(firstPath).Should().Contain("container first message");
        File.ReadAllText(secondPath).Should().Contain("container second message");
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

    #region Inner logger behaviour — mirrors real usage: one per-job logger whose lines carry a Request/Job/Row scope path

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
            loggerFactory.CreateLogger("SlideGenerationWorkload").LogInformation("job started");
        }

        File.Exists(filePath).Should().BeTrue();
    }

    /// <summary>
    ///     Logged message text appears in the file after dispose flushes the sink.
    ///     Mirrors the real pattern: <c>LoggingWorkload.CreateFile(logPath, ["RequestId", "JobId", "RowIndex"])</c>
    ///     followed by a single <c>CreateLogger(nameof(SlideGenerationWorkload)).LogInformation(...)</c> per job.
    /// </summary>
    [Fact]
    public void CreateFile_ValidPath_LoggedMessageAppearsInFile()
    {
        var filePath = NewTempFilePath();
        const string expectedMessage = "created output presentation";

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("SlideGenerationWorkload").LogInformation(expectedMessage);
        }

        File.ReadAllText(filePath).Should().Contain("SlideGenerationWorkload").And.Contain(expectedMessage);
    }

    /// <summary>
    ///     The category name passed to <c>CreateLogger</c> appears in the file as the logger name field.
    ///     Real usage: <c>CreateLogger(nameof(SlideGenerationWorkload))</c> → file shows <c>SlideGenerationWorkload/...</c>
    /// </summary>
    [Fact]
    public void CreateFile_ValidPath_CategoryNameAppearsInFile()
    {
        var filePath = NewTempFilePath();
        const string categoryName = "SlideGenerationWorkload";

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger(categoryName).LogInformation("row filled");
        }

        File.ReadAllText(filePath).Should().Contain(categoryName);
    }

    /// <summary>
    ///     When Request/Job scope properties are pushed onto <see cref="LogContext" /> around a log call,
    ///     the resulting scope path appears in the log line. Real usage: <c>LoggingWorkload</c> pushes
    ///     <c>LogContext.PushProperty("RequestId", ...)</c>/<c>PushProperty("JobId", ...)</c> for the whole
    ///     job, and the per-row loop additionally pushes <c>"RowIndex"</c>.
    /// </summary>
    [Fact]
    public void CreateFile_WithLogContextScope_ScopePathAppearsInFile()
    {
        var filePath = NewTempFilePath();

        using (var loggerFactory = _factory.CreateFile(filePath, ["RequestId", "JobId"]))
        using (LogContext.PushProperty("RequestId", "req-1"))
        using (LogContext.PushProperty("JobId", "job-1"))
        {
            loggerFactory.CreateLogger("SlideGenerationWorkload").LogInformation("row filled");
        }

        File.ReadAllText(filePath).Should().Contain("SlideGenerationWorkload").And.Contain("req-1/job-1");
    }

    /// <summary>
    ///     The <c>onLogEvent</c> callback is invoked once per log line written through the factory, carrying
    ///     the rendered message and scope path — the mechanism <c>LoggingWorkload</c> uses to forward log
    ///     lines to the frontend in real time via <c>ILogNotifier</c>.
    /// </summary>
    [Fact]
    public void CreateFile_WithOnLogEventCallback_InvokedForEachLogLine()
    {
        var filePath = NewTempFilePath();
        var notifications = new List<LogNotification>();

        using (var loggerFactory = _factory.CreateFile(filePath, ["RequestId"], notifications.Add))
        using (LogContext.PushProperty("RequestId", "req-1"))
        {
            loggerFactory.CreateLogger("SlideGenerationWorkload").LogInformation("row filled");
        }

        notifications.Should().ContainSingle(n => n.Message == "row filled" && n.Location == "req-1");
    }

    /// <summary>
    ///     Multiple named loggers from the same factory all write to the same file.
    ///     Mirrors how different parts of the pipeline create named loggers from the one per-job factory.
    /// </summary>
    [Fact]
    public void CreateFile_ValidPath_MultipleNamedLoggers_AllMessagesInFile()
    {
        var filePath = NewTempFilePath();

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("SlideGenerationWorkload").LogInformation("slides created");
            loggerFactory.CreateLogger("PreflightCleanup").LogInformation("cleanup done");
            loggerFactory.CreateLogger("ImageProcessor").LogInformation("image cropped");
        }

        var content = File.ReadAllText(filePath);
        content.Should().Contain("SlideGenerationWorkload").And.Contain("slides created");
        content.Should().Contain("PreflightCleanup").And.Contain("cleanup done");
        content.Should().Contain("ImageProcessor").And.Contain("image cropped");
    }

    /// <summary>Exception message and type appear in the file for Error-level events.</summary>
    [Fact]
    public void CreateFile_ValidPath_LoggedExceptionAppearsInFile()
    {
        var filePath = NewTempFilePath();
        var exception = new InvalidOperationException("Presentation failed to save");

        using (var loggerFactory = _factory.CreateFile(filePath))
        {
            loggerFactory.CreateLogger("SlideGenerationWorkload")
                .LogError(exception, "row failed");
        }

        var content = File.ReadAllText(filePath);
        content.Should().Contain("Presentation failed to save");
        content.Should().Contain(nameof(InvalidOperationException));
    }

    /// <summary>Disposing the factory does not throw even when loggers have written events.</summary>
    [Fact]
    public void CreateFile_ValidPath_DisposeDoesNotThrow()
    {
        var loggerFactory = _factory.CreateFile(NewTempFilePath());
        loggerFactory.CreateLogger("SlideGenerationWorkload").LogInformation("message");

        var act = loggerFactory.Dispose;

        act.Should().NotThrow();
    }

    #endregion
}
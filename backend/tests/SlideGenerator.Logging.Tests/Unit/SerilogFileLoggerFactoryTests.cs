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
using SlideGenerator.Logging.Services;
using Xunit;

namespace SlideGenerator.Logging.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SerilogFileLoggerFactory" />.
/// </summary>
public sealed class SerilogFileLoggerFactoryTests : IDisposable
{
    private readonly SerilogFileLoggerFactory _factory = new(new LoggerConfiguration());
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

    /// <summary>Scope parameter is accepted; the returned factory creates loggers without error.</summary>
    [Fact]
    public void CreateFile_WithScope_LoggerFactoryCreatesNamedLogger()
    {
        using var loggerFactory = _factory.CreateFile(NewTempFilePath(), "Workflow/test-scope");

        var logger = loggerFactory.CreateLogger(nameof(SerilogFileLoggerFactoryTests));
        logger.Should().NotBeNull();
    }

    #region Inner logger behaviour — mirrors real step usage (data.LoggerFactory.CreateLogger(nameof(Step)))

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
    ///     Mirrors the real step pattern: <c>data.LoggerFactory.CreateLogger(nameof(Step)).LogInformation(...)</c>.
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
    ///     When a scope label is provided, it appears in every log line written by loggers from that factory.
    ///     Real usage: <code>CreateFile(path, scope: $"Workflow/{workflowId}")</code>.
    /// </summary>
    [Fact]
    public void CreateFile_WithScope_ScopeAppearsInFile()
    {
        var filePath = NewTempFilePath();
        const string scope = "Workflow/abc-123";

        using (var loggerFactory = _factory.CreateFile(filePath, scope))
        {
            loggerFactory.CreateLogger("AnyStep").LogInformation("step message");
        }

        File.ReadAllText(filePath).Should().Contain("AnyStep").And.Contain("abc-123");
    }

    /// <summary>
    ///     Multiple named loggers from the same factory all write to the same file.
    ///     Mirrors how each step calls <c>data.LoggerFactory.CreateLogger(nameof(Step))</c> independently.
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
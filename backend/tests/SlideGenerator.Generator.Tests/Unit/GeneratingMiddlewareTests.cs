/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: GeneratingMiddlewareTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Serilog.Events;
using SlideGenerator.Document.Slide;
using SlideGenerator.Generator.Abstractions;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Logging.FileLogging;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="global::SlideGenerator.Generator.Middleware.Middleware" />.
/// </summary>
public sealed class MiddlewareTests
{
    private static readonly WorkflowStepDelegate NextResult =
        () => Task.FromResult(ExecutionResult.Next());

    private readonly IStepBody _body = Substitute.For<IStepBody>();
    private readonly IFileLoggerFactory _fileLoggerFactory = Substitute.For<IFileLoggerFactory>();
    private readonly ILogNotifier _logNotifier = Substitute.For<ILogNotifier>();

    private global::SlideGenerator.Generator.Middleware.Middleware CreateMiddleware() => new(_fileLoggerFactory, _logNotifier);

    private static (IStepExecutionContext Context, JobContext Data) CreateContext()
    {
        var data = new JobContext
        {
            Persist = new JobPersistContext
            {
                RequestId = Guid.NewGuid().ToString(),
                Request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()),
                LogPath = Path.Combine(Path.GetTempPath(), "test.log")
            }
        };
        var workflow = new WorkflowInstance { Data = data };
        var context = Substitute.For<IStepExecutionContext>();
        context.Workflow.Returns(workflow);
        return (context, data);
    }

    /// <summary>When workflow data is not Context, next() is called and factory is not touched.</summary>
    [Fact]
    public async Task HandleAsync_NonContextData_CallsNextWithoutInit()
    {
        var workflow = new WorkflowInstance { Data = new object() };
        var context = Substitute.For<IStepExecutionContext>();
        context.Workflow.Returns(workflow);

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _fileLoggerFactory.DidNotReceive().CreateFile(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<Action<LogNotification>?>());
    }

    /// <summary>When LoggerFactory is null, CreateFile is called with the job's LogPath.</summary>
    [Fact]
    public async Task HandleAsync_LoggerFactoryNull_CreatesFromFactory()
    {
        var (context, data) = CreateContext();
        data.Transient.LoggerFactory = null;
        _fileLoggerFactory
            .CreateFile(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<Action<LogNotification>?>())
            .Returns(Substitute.For<ILoggerFactory>());

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _fileLoggerFactory.Received(1).CreateFile(
            data.Persist.LogPath, Arg.Any<IReadOnlyList<string>>(), Arg.Any<Action<LogNotification>?>());
        data.Transient.LoggerFactory.Should().NotBeNull();
    }

    /// <summary>The callback passed to CreateFile forwards each notification to <see cref="ILogNotifier" />.</summary>
    [Fact]
    public async Task HandleAsync_LoggerFactoryNull_CallbackForwardsToLogNotifier()
    {
        var (context, data) = CreateContext();
        data.Transient.LoggerFactory = null;
        Action<LogNotification>? capturedCallback = null;
        _fileLoggerFactory
            .CreateFile(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(),
                Arg.Do<Action<LogNotification>?>(cb => capturedCallback = cb))
            .Returns(Substitute.For<ILoggerFactory>());

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        capturedCallback.Should().NotBeNull();
        var notification = new LogNotification
        {
            Timestamp = DateTimeOffset.UtcNow, Location = "req/job", Level = LogEventLevel.Information, Message = "hello"
        };
        capturedCallback!(notification);

        _logNotifier.Received(1).Publish(Arg.Is<LogEntry>(e =>
            e.Timestamp == notification.Timestamp && e.Path == notification.Location &&
            e.Level == "INF" && e.Info == notification.Message));
    }

    /// <summary>When LoggerFactory is already set, CreateFile is not called again.</summary>
    [Fact]
    public async Task HandleAsync_LoggerFactoryAlreadySet_DoesNotReinitialize()
    {
        var (context, data) = CreateContext();
        var existingFactory = Substitute.For<ILoggerFactory>();
        data.Transient.LoggerFactory = existingFactory;

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _fileLoggerFactory.DidNotReceive().CreateFile(
            Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<Action<LogNotification>?>());
        data.Transient.LoggerFactory.Should().BeSameAs(existingFactory);
    }

    /// <summary>HandleAsync always returns the result produced by next().</summary>
    [Fact]
    public async Task HandleAsync_AlwaysReturnsNextResult()
    {
        var (context, data) = CreateContext();
        data.Transient.LoggerFactory = Substitute.For<ILoggerFactory>();

        var middleware = CreateMiddleware();
        var result = await middleware.HandleAsync(context, _body, NextResult);

        result.Proceed.Should().BeTrue();
    }
}

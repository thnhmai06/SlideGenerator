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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Generator.Infrastructure.Middleware;
using SlideGenerator.Logging.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="GeneratingMiddleware" />.
/// </summary>
public sealed class GeneratingMiddlewareTests
{
    private static readonly WorkflowStepDelegate NextResult =
        () => Task.FromResult(ExecutionResult.Next());

    private readonly IStepBody _body = Substitute.For<IStepBody>();
    private readonly ICoordinatorFactory _coordinatorFactory = Substitute.For<ICoordinatorFactory>();
    private readonly IFileLoggerFactory _fileLoggerFactory = Substitute.For<IFileLoggerFactory>();

    private GeneratingMiddleware CreateMiddleware()
    {
        return new GeneratingMiddleware(_fileLoggerFactory, _coordinatorFactory);
    }

    private static (IStepExecutionContext Context, GeneratingContext Data) CreateGeneratingContext()
    {
        var data = new GeneratingContext
        {
            Request = new GeneratingRequest(1, "Test", PresentationType.Pptx, Path.GetTempPath()),
            WorkflowLogPath = Path.Combine(Path.GetTempPath(), "test.log"),
            WorkflowScope = "TestScope"
        };
        var workflow = new WorkflowInstance { Data = data };
        var context = Substitute.For<IStepExecutionContext>();
        context.Workflow.Returns(workflow);
        return (context, data);
    }

    /// <summary>When workflow data is not GeneratingContext, next() is called and factory is not touched.</summary>
    [Fact]
    public async Task HandleAsync_NonGeneratingContextData_CallsNextWithoutInit()
    {
        var workflow = new WorkflowInstance { Data = new object() };
        var context = Substitute.For<IStepExecutionContext>();
        context.Workflow.Returns(workflow);

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _fileLoggerFactory.DidNotReceive().CreateFile(Arg.Any<string>(), Arg.Any<string?>());
        _coordinatorFactory.DidNotReceive().Create();
    }

    /// <summary>When LoggerFactory is null, CreateFile is called with WorkflowLogPath and the correct scope.</summary>
    [Fact]
    public async Task HandleAsync_LoggerFactoryNull_CreatesFromFactory()
    {
        var (context, data) = CreateGeneratingContext();
        data.LoggerFactory = null;
        _fileLoggerFactory
            .CreateFile(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Substitute.For<ILoggerFactory>());

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _fileLoggerFactory.Received(1).CreateFile(
            data.WorkflowLogPath,
            $"Workflow/{data.WorkflowScope}");
        data.LoggerFactory.Should().NotBeNull();
    }

    /// <summary>When LoggerFactory is already set, CreateFile is not called again.</summary>
    [Fact]
    public async Task HandleAsync_LoggerFactoryAlreadySet_DoesNotReinitialize()
    {
        var (context, data) = CreateGeneratingContext();
        var existingFactory = Substitute.For<ILoggerFactory>();
        data.LoggerFactory = existingFactory;

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _fileLoggerFactory.DidNotReceive().CreateFile(Arg.Any<string>(), Arg.Any<string?>());
        data.LoggerFactory.Should().BeSameAs(existingFactory);
    }

    /// <summary>When AssetCoordinator is null, coordinatorFactory.Create() is called once.</summary>
    [Fact]
    public async Task HandleAsync_AssetCoordinatorNull_CreatesFromFactory()
    {
        var (context, data) = CreateGeneratingContext();
        data.LoggerFactory = Substitute.For<ILoggerFactory>();
        data.AssetCoordinator = null;
        var coordinator = Substitute.For<ICoordinator>();
        _coordinatorFactory.Create().Returns(coordinator);

        var middleware = CreateMiddleware();
        await middleware.HandleAsync(context, _body, NextResult);

        _coordinatorFactory.Received(1).Create();
        data.AssetCoordinator.Should().BeSameAs(coordinator);
    }

    /// <summary>HandleAsync always returns the result produced by next().</summary>
    [Fact]
    public async Task HandleAsync_AlwaysReturnsNextResult()
    {
        var (context, data) = CreateGeneratingContext();
        data.LoggerFactory = Substitute.For<ILoggerFactory>();
        data.AssetCoordinator = Substitute.For<ICoordinator>();

        var middleware = CreateMiddleware();
        var result = await middleware.HandleAsync(context, _body, NextResult);

        result.Proceed.Should().BeTrue();
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: StepHelper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using NSubstitute;
using SlideGenerator.Document.Models.Slide;
using SlideGenerator.Generator.Domain.Models.Data;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Tests.Unit.Helpers;

/// <summary>
///     Factory methods for constructing test doubles used across Generator step tests.
/// </summary>
internal static class StepHelper
{
    /// <summary>
    ///     Creates a matched pair of a mocked <see cref="IStepExecutionContext" /> and a real
    ///     <see cref="JobContext" /> wired through a <see cref="WorkflowInstance" />.
    ///     The context's <see cref="TransientContext.LoggerFactory" /> is pre-configured with
    ///     a substitute that returns a no-op <see cref="ILogger" /> for any category name.
    /// </summary>
    internal static (IStepExecutionContext Ctx, JobContext Data) CreateContextPair()
    {
        var (ctx, data, _) = CreateContextPairWithLogger();
        return (ctx, data);
    }

    /// <summary>
    ///     Like <see cref="CreateContextPair" /> but also returns the underlying <see cref="ILogger" />
    ///     substitute so tests can assert on logged calls.
    /// </summary>
    internal static (IStepExecutionContext Ctx, JobContext Data, ILogger MockLogger)
        CreateContextPairWithLogger()
    {
        var mockLogger = Substitute.For<ILogger>();
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
        mockLoggerFactory.CreateLogger(Arg.Any<string>()).Returns(mockLogger);

        var data = new JobContext
        {
            Persist = new JobPersistContext
            {
                RequestId = Guid.NewGuid().ToString(),
                Request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()),
                LogPath = Path.Combine(Path.GetTempPath(), "test.log"),
                Specification = new JobSpecification
                {
                    Source = new WorkbookRef("wb1", "ws1"),
                    Template = new PresentationRef("ppt1", "slide1"),
                    MapNodeId = "map1",
                    OutputPath = Path.Combine(Path.GetTempPath(), "output.pptx")
                }
            }
        };
        data.Transient.LoggerFactory = mockLoggerFactory;

        var workflow = new WorkflowInstance { Data = data };

        var ctx = Substitute.For<IStepExecutionContext>();
        ctx.Workflow.Returns(workflow);
        ctx.CancellationToken.Returns(CancellationToken.None);

        return (ctx, data, mockLogger);
    }
}

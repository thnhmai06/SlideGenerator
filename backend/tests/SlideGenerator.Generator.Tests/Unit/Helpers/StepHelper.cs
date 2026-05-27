/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: StepHelper.cs
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

using Microsoft.Extensions.Logging;
using NSubstitute;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Summarization.Domain.Models.Recipes;
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
    ///     <see cref="GeneratingContext" /> wired through a <see cref="WorkflowInstance" />.
    ///     The context's <see cref="GeneratingContext.Logger" /> is pre-configured with a no-op
    ///     <see cref="IDisposable" /> scope so steps can call <c>BeginScope</c> freely.
    /// </summary>
    internal static (IStepExecutionContext Ctx, GeneratingContext Data) CreateContextPair()
    {
        var data = new GeneratingContext
        {
            Request = new GeneratingRequest(
                1,
                "Test",
                PresentationType.Pptx,
                Path.GetTempPath()),
            WorkflowLogPath = Path.Combine(Path.GetTempPath(), "test.log"),
            WorkflowScope = "TestScope",
            Logger = Substitute.For<ILogger>()
        };

        var workflow = new WorkflowInstance { Data = data };

        var ctx = Substitute.For<IStepExecutionContext>();
        ctx.Workflow.Returns(workflow);

        return (ctx, data);
    }

    /// <summary>
    ///     Builds a minimal <see cref="SheetContext" /> using in-memory paths suitable for unit tests.
    /// </summary>
    internal static SheetContext BuildSheetContext(string? outputPath = null)
    {
        var sheetId = new SheetIdentifier(
            Path.Combine(Path.GetTempPath(), "workbook.xlsx"), "Sheet1");
        var slideId = new SlideIdentifier(
            Path.Combine(Path.GetTempPath(), "template.pptx"), 1);
        var outputId = new PresentationIdentifier(
            outputPath ?? Path.Combine(Path.GetTempPath(), "output.pptx"));
        var node = new MapNode(
            new HashSet<SheetIdentifier> { sheetId },
            slideId,
            [],
            []);
        return new SheetContext(sheetId, slideId, node, outputId);
    }
}
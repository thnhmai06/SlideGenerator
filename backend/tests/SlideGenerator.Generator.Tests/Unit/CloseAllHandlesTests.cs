/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: CloseAllHandlesTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Tests.Unit.Helpers;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="CloseAllHandles" />, verifying that the step disposes and clears
///     all open workbook, template, and output presentation handles stored in
///     <see cref="SlideGenerator.Generator.Domain.Models.Contexts.GeneratingContext" />.
/// </summary>
public sealed class CloseAllHandlesTests
{
    private readonly CloseAllHandles _step = new();

    #region Handle disposal

    /// <summary>
    ///     Verifies that <see cref="CloseAllHandles.Run" /> empties all handle collections when
    ///     the context contains open workbook and presentation handles.
    /// </summary>
    [Fact]
    public void Run_WithOpenHandles_ClearsAllHandleCollections()
    {
        var (ctx, data) = StepHelper.CreateContextPair();

        var workbook = Substitute.For<IReadOnlyWorkbook>();
        var template = Substitute.For<IReadOnlyPresentation>();
        var output = Substitute.For<IPresentation>();

        var bookId = new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "workbook.xlsx"));
        var templateId = new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx"));
        var outputId = new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "output.pptx"));

        data.WorkbookHandles.TryAdd(bookId, workbook);
        data.TemplateHandles.TryAdd(templateId, template);
        data.OutputHandles.TryAdd(outputId, output);

        _step.Run(ctx);

        data.WorkbookHandles.Should().BeEmpty();
        data.TemplateHandles.Should().BeEmpty();
        data.OutputHandles.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="CloseAllHandles.Run" /> calls <see cref="IDisposable.Dispose" /> on
    ///     each workbook handle it closes.
    /// </summary>
    [Fact]
    public void Run_WithOpenHandles_DisposesEachHandle()
    {
        var (ctx, data) = StepHelper.CreateContextPair();

        var workbook = Substitute.For<IReadOnlyWorkbook>();
        var bookId = new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "workbook.xlsx"));
        data.WorkbookHandles.TryAdd(bookId, workbook);

        _step.Run(ctx);

        workbook.Received(1).Dispose();
    }

    /// <summary>
    ///     Verifies that <see cref="CloseAllHandles.Run" /> completes without throwing when all
    ///     handle collections are already empty.
    /// </summary>
    [Fact]
    public void Run_EmptyHandleCollections_CompletesWithoutThrowing()
    {
        var (ctx, _) = StepHelper.CreateContextPair();

        var act = () => _step.Run(ctx);

        act.Should().NotThrow();
    }

    #endregion
}
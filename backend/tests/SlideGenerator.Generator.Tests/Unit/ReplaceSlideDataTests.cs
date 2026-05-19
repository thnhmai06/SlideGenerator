/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: ReplaceSlideDataTests.cs
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

using NSubstitute;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Generator.Tests.Unit.Helpers;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="ReplaceSlideData" />, covering the early-exit path when no replacements
///     are present and the text-composition path when replacements exist.
/// </summary>
public sealed class ReplaceSlideDataTests
{
    private readonly IGateLocker _gateLocker = Substitute.For<IGateLocker>();
    private readonly IPresentationProvider _presentationProvider = Substitute.For<IPresentationProvider>();
    private readonly ITextComposer _textComposer = Substitute.For<ITextComposer>();

    #region With text replacements

    /// <summary>
    ///     Verifies that <see cref="ReplaceSlideData.RunAsync" /> calls <see cref="ITextComposer.Compose" />
    ///     for each shape on the target slide when text replacements are present.
    /// </summary>
    [Fact]
    public async Task RunAsync_WithTextReplacements_ComposesEachNamedShape()
    {
        var (ctx, data) = StepHelper.CreateContextPair();

        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pptx");
        var sheetCtx = StepHelper.BuildSheetContext(outputPath);
        var slideCtx = new SlideContext(sheetCtx, 1);
        slideCtx.TextReplacements.Add("name", "World");

        var mockShape = Substitute.For<IShape>();
        mockShape.Name.Returns("Placeholder1");
        mockShape.Paragraph.Returns([]);

        var mockSlide = Substitute.For<ISlide>();
        mockSlide.Shapes.Returns([mockShape]);

        var mockPresentation = Substitute.For<IPresentation>();
        mockPresentation.Slides.Returns([mockSlide]);

        data.OutputHandles.TryAdd(sheetCtx.OutputIdentifier, mockPresentation);

        var step = new ReplaceSlideData(_gateLocker, _textComposer, _presentationProvider)
        {
            Task = slideCtx
        };

        await step.RunAsync(ctx);

        _textComposer.Received(1).Compose(mockShape, Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    #endregion

    #region No replacements — early exit

    /// <summary>
    ///     Verifies that <see cref="ReplaceSlideData.RunAsync" /> returns immediately without acquiring
    ///     the gate lock when both <see cref="SlideContext.TextReplacements" /> and
    ///     <see cref="SlideContext.ImageReplacements" /> are empty.
    /// </summary>
    [Fact]
    public async Task RunAsync_NoReplacements_DoesNotAcquireGate()
    {
        var (ctx, _) = StepHelper.CreateContextPair();
        var sheetCtx = StepHelper.BuildSheetContext();
        var slideCtx = new SlideContext(sheetCtx, 1);

        var step = new ReplaceSlideData(_gateLocker, _textComposer, _presentationProvider)
        {
            Task = slideCtx
        };

        await step.RunAsync(ctx);

        await _gateLocker.DidNotReceive().AcquireAsync(Arg.Any<GateType>());
    }

    /// <summary>
    ///     Verifies that <see cref="ReplaceSlideData.RunAsync" /> does not invoke
    ///     <see cref="ITextComposer.Compose" /> when no text replacements are present.
    /// </summary>
    [Fact]
    public async Task RunAsync_NoReplacements_DoesNotCallTextComposer()
    {
        var (ctx, _) = StepHelper.CreateContextPair();
        var sheetCtx = StepHelper.BuildSheetContext();
        var slideCtx = new SlideContext(sheetCtx, 1);

        var step = new ReplaceSlideData(_gateLocker, _textComposer, _presentationProvider)
        {
            Task = slideCtx
        };

        await step.RunAsync(ctx);

        _textComposer.DidNotReceive().Compose(Arg.Any<IShape>(), Arg.Any<IReadOnlyDictionary<string, string>>());
    }

    #endregion
}
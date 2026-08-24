/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: SlideCanvasViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Presentations;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="SlideCanvasViewModel.Load" />'s overlay derivation via
///     <see cref="BindingDisplayResolver" /> — a real saved binding wins as Assigned; otherwise the shape
///     name is matched against <c>availableColumns</c> the same way a text placeholder would be.
/// </summary>
public sealed class SlideCanvasViewModelTests
{
    private static readonly PresentationIdentifier Presentation = new("template.pptx");
    private static readonly SlideIdentifier Slide = new(1);

    [Fact]
    public void Load_ShapeReferencedByImageInstruction_IsAssignedWithColumnLabel()
    {
        var boundShape = new ShapeIdentifier("Avatar");
        var slide = new SlideSummary(Presentation, Slide, [], [new ShapeSummary(Slide, boundShape, new RectangleF(0, 0, 10, 10))], null, new SizeF(100, 100));
        var instructions = new List<ImageInstruction>
        {
            new(new HashSet<ShapeIdentifier> { boundShape }, [new ColumnIdentifier("PhotoUrl")], new ImageEditInstruction([]))
        };

        var vm = new SlideCanvasViewModel();
        vm.Load(slide, instructions, []);

        var overlay = vm.Overlays.Should().ContainSingle().Subject;
        overlay.Binding.State.Should().Be(BindingDisplayState.Assigned);
        overlay.Binding.Column.Should().Be("PhotoUrl");
    }

    [Fact]
    public void Load_ShapeNameExactlyMatchesColumn_IsAssignedByAutoBind()
    {
        var shape = new ShapeIdentifier("Avatar");
        var slide = new SlideSummary(Presentation, Slide, [], [new ShapeSummary(Slide, shape, new RectangleF(0, 0, 10, 10))], null, new SizeF(100, 100));

        var vm = new SlideCanvasViewModel();
        vm.Load(slide, [], ["Avatar"]);

        var overlay = vm.Overlays.Should().ContainSingle().Subject;
        overlay.Binding.State.Should().Be(BindingDisplayState.Assigned);
        overlay.Binding.Column.Should().Be("Avatar");
    }

    [Fact]
    public void Load_ShapeNameNoCandidateColumn_IsUnassignedWithNoLabel()
    {
        var slide = new SlideSummary(Presentation, Slide, [], [new ShapeSummary(Slide, new ShapeIdentifier("Logo"), new RectangleF(0, 0, 10, 10))], null, new SizeF(100, 100));

        var vm = new SlideCanvasViewModel();
        vm.Load(slide, [], ["Avatar"]);

        var overlay = vm.Overlays.Should().ContainSingle().Subject;
        overlay.Binding.State.Should().Be(BindingDisplayState.Unassigned);
        overlay.Binding.Column.Should().BeNull();
    }

    [Fact]
    public void Load_ReplacesPreviousOverlays()
    {
        var slide1 = new SlideSummary(Presentation, Slide, [], [new ShapeSummary(Slide, new ShapeIdentifier("A"), new RectangleF(0, 0, 10, 10))], null, new SizeF(100, 100));
        var slide2 = new SlideSummary(Presentation, new SlideIdentifier(2), [], [], null, new SizeF(100, 100));

        var vm = new SlideCanvasViewModel();
        vm.Load(slide1, [], []);
        vm.Load(slide2, [], []);

        vm.Overlays.Should().BeEmpty();
    }
}

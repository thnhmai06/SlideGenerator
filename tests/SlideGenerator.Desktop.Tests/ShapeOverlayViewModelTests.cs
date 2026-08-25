/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: ShapeOverlayViewModelTests.cs
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
using SlideGenerator.Image.Cropping;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Presentations;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="ShapeOverlayViewModel" />'s ROI try-order editing (inspector, P4.4) — the
///     move/remove commands operate on the live <see cref="ShapeOverlayViewModel.RoiOptions" /> list, and
///     <see cref="ShapeOverlayViewModel.EditInstruction" /> must always reflect its current order.
/// </summary>
public sealed class ShapeOverlayViewModelTests
{
    private static ShapeOverlayViewModel CreateOverlay(params RoiOption[] roiOptions)
    {
        var shape = new ShapeSummary(new SlideIdentifier(1), new ShapeIdentifier("Avatar"), new RectangleF(0, 0, 10, 10));
        var binding = new BindingDisplay("Avatar", BindingDisplayState.Unassigned, null, []);
        return new ShapeOverlayViewModel(shape, binding, new ImageEditInstruction(roiOptions), null, []);
    }

    [Fact]
    public void MoveRoiUpCommand_SecondOption_SwapsToFirst()
    {
        var first = new InterestOption { Type = InterestType.Attention };
        var second = new AnchorOption { Type = AnchorType.Image };
        var overlay = CreateOverlay(first, second);

        overlay.MoveRoiUpCommand.Execute(second);

        overlay.RoiOptions.Should().Equal(second, first);
    }

    [Fact]
    public void MoveRoiUpCommand_AlreadyFirst_IsNoOp()
    {
        var first = new InterestOption { Type = InterestType.Attention };
        var second = new AnchorOption { Type = AnchorType.Image };
        var overlay = CreateOverlay(first, second);

        overlay.MoveRoiUpCommand.Execute(first);

        overlay.RoiOptions.Should().Equal(first, second);
    }

    [Fact]
    public void MoveRoiDownCommand_FirstOption_SwapsToSecond()
    {
        var first = new InterestOption { Type = InterestType.Attention };
        var second = new AnchorOption { Type = AnchorType.Image };
        var overlay = CreateOverlay(first, second);

        overlay.MoveRoiDownCommand.Execute(first);

        overlay.RoiOptions.Should().Equal(second, first);
    }

    [Fact]
    public void RemoveRoiCommand_RemovesFromList()
    {
        var first = new InterestOption { Type = InterestType.Attention };
        var second = new AnchorOption { Type = AnchorType.Image };
        var overlay = CreateOverlay(first, second);

        overlay.RemoveRoiCommand.Execute(first);

        overlay.RoiOptions.Should().Equal(second);
    }

    [Fact]
    public void EditInstruction_ReflectsCurrentRoiOptionsOrder()
    {
        var first = new InterestOption { Type = InterestType.Attention };
        var second = new AnchorOption { Type = AnchorType.Image };
        var overlay = CreateOverlay(first, second);

        overlay.MoveRoiDownCommand.Execute(first);

        overlay.EditInstruction.RoiOptions.Should().Equal(second, first);
    }
}

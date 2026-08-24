/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ShapeOverlayViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Presentations;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>One image shape's overlay on the <see cref="SlideCanvasViewModel" />. Bounds are in slide pixel
///     space (<see cref="ShapeSummary.Bounds" />); <see cref="ScreenBounds" /> is the canvas-relative rectangle
///     computed by <see cref="SlideCanvasGeometry.ScaleBounds" /> whenever the host control resizes.
///     <see cref="EditInstruction" />/<see cref="FallbackImagePath" /> carry the shape's existing ROI/fallback
///     config forward — no inspector edits them yet (P4.4 continuation), but <see cref="SlideCanvasViewModel.ToImageInstructions" />
///     must not silently drop them when projecting edits back.</summary>
public sealed partial class ShapeOverlayViewModel(ShapeSummary shape, BindingDisplay binding,
    ImageEditInstruction editInstruction, string? fallbackImagePath) : ObservableObject
{
    /// <summary>Gets the underlying shape (slide/name/bounds in slide pixel space).</summary>
    public ShapeSummary Shape { get; } = shape;

    /// <summary>Gets the shape's existing ROI fallback chain, carried through untouched until an inspector can edit it.</summary>
    public ImageEditInstruction EditInstruction { get; } = editInstruction;

    /// <summary>Gets the shape's existing fallback image path, carried through untouched until an inspector can edit it.</summary>
    public string? FallbackImagePath { get; } = fallbackImagePath;

    [ObservableProperty] private BindingDisplay _binding = binding;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Rect _screenBounds;
}

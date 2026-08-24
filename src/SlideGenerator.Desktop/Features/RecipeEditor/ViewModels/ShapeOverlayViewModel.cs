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
using SlideGenerator.Summarizer.Presentations;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>One image shape's overlay on the <see cref="SlideCanvasViewModel" />. Bounds are in slide pixel
///     space (<see cref="ShapeSummary.Bounds" />); <see cref="ScreenBounds" /> is the canvas-relative rectangle
///     computed by <see cref="SlideCanvasGeometry.ScaleBounds" /> whenever the host control resizes.</summary>
public sealed partial class ShapeOverlayViewModel(ShapeSummary shape, BindingDisplay binding) : ObservableObject
{
    /// <summary>Gets the underlying shape (slide/name/bounds in slide pixel space).</summary>
    public ShapeSummary Shape { get; } = shape;

    [ObservableProperty] private BindingDisplay _binding = binding;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Rect _screenBounds;
}

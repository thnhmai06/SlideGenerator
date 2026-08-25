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

using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Presentations;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>One image shape's overlay on the <see cref="SlideCanvasViewModel" />. Bounds are in slide pixel
///     space (<see cref="ShapeSummary.Bounds" />); <see cref="ScreenBounds" /> is the canvas-relative rectangle
///     computed by <see cref="SlideCanvasGeometry.ScaleBounds" /> whenever the host control resizes.
///     <see cref="RoiOptions" />/<see cref="FallbackImagePath" /> start from the shape's existing
///     <see cref="ImageEditInstruction" /> and are editable by the inspector (P4.4) — the coordinator's
///     Move/Remove-Roi and pick-fallback commands mutate them directly; <see cref="EditInstruction" /> rebuilds
///     the record from the live <see cref="RoiOptions" /> list each time <see cref="SlideCanvasViewModel.ToImageInstructions" />
///     reads it.</summary>
public sealed partial class ShapeOverlayViewModel(ShapeSummary shape, BindingDisplay binding,
    ImageEditInstruction editInstruction, string? fallbackImagePath, IReadOnlyList<string> allColumns) : ObservableObject
{
    /// <summary>Gets the underlying shape (slide/name/bounds in slide pixel space).</summary>
    public ShapeSummary Shape { get; } = shape;

    /// <summary>Gets every column visible to this mapping's worksheet sources, for the double-click quick-assign dropdown.</summary>
    public IReadOnlyList<string> AllColumns { get; } = allColumns;

    /// <summary>Gets the shape's ROI fallback chain, in try-order — reorderable/removable by the inspector.</summary>
    public ObservableCollection<RoiOption> RoiOptions { get; } = new(editInstruction.RoiOptions);

    /// <summary>Gets the current <see cref="ImageEditInstruction" />, rebuilt from the live <see cref="RoiOptions" /> list.</summary>
    public ImageEditInstruction EditInstruction => new(RoiOptions.ToList());

    [ObservableProperty] private BindingDisplay _binding = binding;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Rect _screenBounds;
    [ObservableProperty] private string? _fallbackImagePath = fallbackImagePath;

    /// <summary>Moves a ROI option one step earlier in the try-order.</summary>
    [RelayCommand]
    private void MoveRoiUp(RoiOption option)
    {
        var index = RoiOptions.IndexOf(option);
        if (index > 0) RoiOptions.Move(index, index - 1);
    }

    /// <summary>Moves a ROI option one step later in the try-order.</summary>
    [RelayCommand]
    private void MoveRoiDown(RoiOption option)
    {
        var index = RoiOptions.IndexOf(option);
        if (index >= 0 && index < RoiOptions.Count - 1) RoiOptions.Move(index, index + 1);
    }

    /// <summary>Removes a ROI option from the try-order.</summary>
    [RelayCommand]
    private void RemoveRoi(RoiOption option)
    {
        RoiOptions.Remove(option);
    }
}

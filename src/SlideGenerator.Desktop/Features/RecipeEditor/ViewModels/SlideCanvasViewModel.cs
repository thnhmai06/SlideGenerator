/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: SlideCanvasViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.Drawing;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Presentations;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Owns one slide's preview image and its image-shape overlays (P4.2). <see cref="Load" /> is pure
///     transform over already-fetched <see cref="SlideSummary" />/<see cref="ImageInstruction" /> data — no
///     I/O here, that belongs to <c>ISummaryCache</c> upstream. <see cref="CanvasSize" /> is pushed in by the
///     hosting view (its actual rendered size, since Avalonia doesn't expose that as a bindable source) and
///     drives <see cref="SlideCanvasGeometry.ScaleBounds" /> for every overlay whenever either it or the slide
///     changes.
/// </summary>
public sealed partial class SlideCanvasViewModel : ObservableObject
{
    [ObservableProperty] private Bitmap? _preview;
    [ObservableProperty] private SizeF _slideSize;
    [ObservableProperty] private global::Avalonia.Size _canvasSize;
    [ObservableProperty] private ShapeOverlayViewModel? _selectedOverlay;

    /// <summary>Gets the image-shape overlays for the currently loaded slide.</summary>
    public ObservableCollection<ShapeOverlayViewModel> Overlays { get; } = [];

    /// <summary>
    ///     Loads a slide's preview and shape overlays. Each shape's state comes from
    ///     <see cref="BindingDisplayResolver" />: a real binding in <paramref name="imageInstructions" /> wins
    ///     as Assigned; otherwise <paramref name="availableColumns" /> (every column visible to this mapping's
    ///     worksheet sources) feeds the same auto-bind suggestion used for text placeholders.
    ///     <paramref name="touchedShapeNames" /> holds shapes the user has already confirmed/changed via the
    ///     canvas's double-click dropdown (not yet wired — defaults to none touched).
    /// </summary>
    public void Load(SlideSummary slide, IReadOnlyList<ImageInstruction> imageInstructions,
        IReadOnlyList<string> availableColumns, IReadOnlySet<string>? touchedShapeNames = null)
    {
        Preview?.Dispose();
        Preview = slide.Preview is { Length: > 0 } bytes ? new Bitmap(new MemoryStream(bytes)) : null;
        SlideSize = slide.SlideSize;

        Overlays.Clear();
        foreach (var shape in slide.ImageShapes)
        {
            var instruction = imageInstructions.FirstOrDefault(i => i.Shapes.Contains(shape.Shape));
            var existingColumn = instruction?.Columns.FirstOrDefault()?.ColumnName;
            var touched = touchedShapeNames?.Contains(shape.Shape.ShapeName) ?? false;
            var binding = BindingDisplayResolver.Resolve(shape.Shape.ShapeName, existingColumn, availableColumns, touched);

            var overlay = new ShapeOverlayViewModel(shape, binding)
            {
                ScreenBounds = SlideCanvasGeometry.ScaleBounds(SlideSize, CanvasSize, shape.Bounds)
            };
            Overlays.Add(overlay);
        }
    }

    partial void OnCanvasSizeChanged(global::Avalonia.Size value)
    {
        RecomputeOverlayBounds();
    }

    private void RecomputeOverlayBounds()
    {
        foreach (var overlay in Overlays)
            overlay.ScreenBounds = SlideCanvasGeometry.ScaleBounds(SlideSize, CanvasSize, overlay.Shape.Bounds);
    }

    [RelayCommand]
    private void Select(ShapeOverlayViewModel overlay)
    {
        if (SelectedOverlay is not null) SelectedOverlay.IsSelected = false;
        SelectedOverlay = overlay;
        overlay.IsSelected = true;
    }
}

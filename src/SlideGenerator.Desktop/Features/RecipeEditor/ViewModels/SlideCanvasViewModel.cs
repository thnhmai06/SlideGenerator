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
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
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
    private HashSet<string> _touched = [];

    [ObservableProperty] private Bitmap? _preview;
    [ObservableProperty] private SizeF _slideSize;
    [ObservableProperty] private global::Avalonia.Size _canvasSize;
    [ObservableProperty] private ShapeOverlayViewModel? _selectedOverlay;

    /// <summary>Raised when an overlay's column is set via <see cref="SetOverlayColumn" /> (not by <see cref="Load" />)
    ///     — the coordinator marks the recipe dirty on this.</summary>
    public event Action? Changed;

    /// <summary>Gets the image-shape overlays for the currently loaded slide.</summary>
    public ObservableCollection<ShapeOverlayViewModel> Overlays { get; } = [];

    /// <summary>Gets the count of each <see cref="BindingDisplayState" /> across <see cref="Overlays" /> — the
    ///     image-shape half of the Advanced warning strip's summary (text half: <see cref="TextBindingsViewModel.Summary" />).</summary>
    public (int Assigned, int Suggested, int NeedsSelection, int Unassigned) Summary =>
        BindingDisplayResolver.Summarize(Overlays.Select(o => o.Binding).ToList());

    /// <summary>
    ///     Loads a slide's preview and shape overlays. Each shape's state comes from
    ///     <see cref="BindingDisplayResolver" />: a real binding in <paramref name="imageInstructions" /> wins
    ///     as Assigned; otherwise <paramref name="availableColumns" /> (every column visible to this mapping's
    ///     worksheet sources) feeds the same auto-bind suggestion used for text placeholders.
    ///     <paramref name="touchedShapeNames" />, when given, is the caller-owned touched set to read/mutate —
    ///     pass the same set back in across mapping switches so a confirmed Normalized binding doesn't revert to
    ///     Suggested; omit it for a fresh, this-load-only set.
    /// </summary>
    public void Load(SlideSummary slide, IReadOnlyList<ImageInstruction> imageInstructions,
        IReadOnlyList<string> availableColumns, HashSet<string>? touchedShapeNames = null)
    {
        _touched = touchedShapeNames ?? [];
        Preview?.Dispose();
        Preview = slide.Preview is { Length: > 0 } bytes ? new Bitmap(new MemoryStream(bytes)) : null;
        SlideSize = slide.SlideSize;

        Overlays.Clear();
        foreach (var shape in slide.ImageShapes)
        {
            var instruction = imageInstructions.FirstOrDefault(i => i.Shapes.Contains(shape.Shape));
            var existingColumn = instruction?.Columns.FirstOrDefault()?.ColumnName;
            var touched = _touched.Contains(shape.Shape.ShapeName);
            var binding = BindingDisplayResolver.Resolve(shape.Shape.ShapeName, existingColumn, availableColumns, touched);
            var editInstruction = instruction?.ImageEditInstruction ?? new ImageEditInstruction([]);

            var overlay = new ShapeOverlayViewModel(shape, binding, editInstruction, instruction?.FallbackImagePath, availableColumns)
            {
                ScreenBounds = SlideCanvasGeometry.ScaleBounds(SlideSize, CanvasSize, shape.Bounds)
            };
            Overlays.Add(overlay);
        }

        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>Sets (or overrides) an overlay's column via the canvas double-click quick-assign dropdown,
    ///     marking it touched so a Suggested state never reappears for it.</summary>
    public void SetOverlayColumn(ShapeOverlayViewModel overlay, string column)
    {
        _touched.Add(overlay.Shape.Shape.ShapeName);
        overlay.Binding = new BindingDisplay(overlay.Shape.Shape.ShapeName, BindingDisplayState.Assigned, column, []);
        OnPropertyChanged(nameof(Summary));
        Changed?.Invoke();
    }

    /// <summary>Projects every overlay with a column into the flat <see cref="ImageInstruction" /> list a
    ///     <c>Mapping</c> needs, preserving each shape's existing <see cref="ImageEditInstruction" />/fallback
    ///     path (see <see cref="ShapeOverlayViewModel" />) since no inspector edits them yet.</summary>
    public IReadOnlyList<ImageInstruction> ToImageInstructions()
    {
        return Overlays
            .Where(o => o.Binding.Column is not null)
            .Select(o => new ImageInstruction(
                new HashSet<ShapeIdentifier> { o.Shape.Shape },
                [new ColumnIdentifier(o.Binding.Column!)],
                o.EditInstruction,
                o.FallbackImagePath))
            .ToList();
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

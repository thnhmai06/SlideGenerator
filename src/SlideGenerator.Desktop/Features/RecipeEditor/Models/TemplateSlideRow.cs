/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TemplateSlideRow.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia.Media.Imaging;
using SlideGenerator.Summarizer.Presentations;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Models;

/// <summary>One slide row in the template picker (P4.4 "add mapping") — pre-converts <see cref="SlideSummary.Preview" />
///     to a <see cref="Bitmap" /> once, matching how <c>SlideCanvasViewModel</c> already does the byte[]-to-Bitmap
///     conversion at the ViewModel layer rather than via an XAML converter.</summary>
public sealed class TemplateSlideRow(SlideSummary slide, Bitmap? preview)
{
    /// <summary>Gets the underlying slide summary.</summary>
    public SlideSummary Slide { get; } = slide;

    /// <summary>Gets the decoded preview bitmap, or <see langword="null" /> if the slide has no preview.</summary>
    public Bitmap? Preview { get; } = preview;
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ImageContext.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Recipe.Domain.Models.Summary;

namespace SlideGenerator.Generator.Domain.Models.Contexts;

/// <summary>
///     Represents a comprehensive context for downloading and editing a single image.
/// </summary>
public sealed class ImageContext(
    SheetIdentifier sheet,
    int rowIndex,
    string columnName,
    string shapeName,
    string? sourceUrl,
    string downloadPath,
    string editPath,
    double width,
    double height,
    ImageEdits editOptions,
    string? fallbackImagePath = null)
{
    /// <summary>Gets the source sheet identifier.</summary>
    public SheetIdentifier Sheet { get; } = sheet;

    /// <summary>Gets the 1-based row index in the sheet.</summary>
    public int RowIndex { get; } = rowIndex;

    /// <summary>Gets the name of the column providing the image.</summary>
    public string ColumnName { get; } = columnName;

    /// <summary>Gets the target shape name in the presentation.</summary>
    public string ShapeName { get; } = shapeName;

    /// <summary>
    ///     Gets the raw URL or file path string from the Excel cell.
    ///     <see langword="null" /> when the cell was empty or contained only whitespace.
    ///     URI normalisation and local-path detection are deferred to the acquire step.
    /// </summary>
    public string? SourceUrl { get; } = sourceUrl;

    /// <summary>Gets the local path where the raw image is downloaded.</summary>
    public string DownloadPath { get; } = downloadPath;

    /// <summary>Gets the local path where the edited image is saved.</summary>
    public string EditPath { get; } = editPath;

    /// <summary>Gets the target width of the shape in points.</summary>
    public double Width { get; } = width;

    /// <summary>Gets the target height of the shape in points.</summary>
    public double Height { get; } = height;

    /// <summary>Gets the processing options for the image.</summary>
    public ImageEdits ImageEdits { get; } = editOptions;

    /// <summary>Gets the path to the fallback image to use if the primary source fails.</summary>
    public string? FallbackImagePath { get; } = fallbackImagePath;
}
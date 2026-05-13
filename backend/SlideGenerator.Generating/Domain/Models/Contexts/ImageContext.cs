/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: ImageContext.cs
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
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Generating.Domain.Models.Dto;

namespace SlideGenerator.Generating.Domain.Models.Contexts;

/// <summary>
///     Represents a comprehensive context for downloading and editing a single image.
/// </summary>
public sealed class ImageContext(
    SheetIdentifier sheet,
    int rowIndex,
    string columnName,
    string shapeName,
    Uri? sourceUri,
    string downloadPath,
    string editPath,
    double width,
    double height,
    EditOptions editOptions,
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

    /// <summary>Gets the URI to download the image from, if available.</summary>
    public Uri? SourceUri { get; } = sourceUri;

    /// <summary>Gets the local path where the raw image is downloaded.</summary>
    public string DownloadPath { get; } = downloadPath;

    /// <summary>Gets the local path where the edited image is saved.</summary>
    public string EditPath { get; } = editPath;

    /// <summary>Gets the target width of the shape in points.</summary>
    public double Width { get; } = width;

    /// <summary>Gets the target height of the shape in points.</summary>
    public double Height { get; } = height;

    /// <summary>Gets the processing options for the image.</summary>
    public EditOptions EditOptions { get; } = editOptions;

    /// <summary>Gets the path to the fallback image to use if the primary source fails.</summary>
    public string? FallbackImagePath { get; } = fallbackImagePath;
}


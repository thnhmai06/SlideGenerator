/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IShape.cs
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

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a shape on a PowerPoint slide that can be modified.
/// </summary>
public interface IShape : IReadOnlyShape
{
    /// <summary>
    ///     Gets or sets the raw image data of the shape.
    /// </summary>
    new byte[]? ImageData { get; set; }

    /// <summary>
    ///     Gets the collection of paragraphs contained within the shape.
    /// </summary>
    new IEnumerable<IParagraph> Paragraph { get; }

    /// <inheritdoc />
    byte[]? IReadOnlyShape.ImageData => ImageData;

    /// <inheritdoc />
    IEnumerable<IReadOnlyParagraph> IReadOnlyShape.Paragraph => Paragraph;

    /// <summary>
    ///     Appends a new empty paragraph to this shape and returns it.
    /// </summary>
    IParagraph AddParagraph();

    /// <summary>
    ///     Clears all paragraphs from the shape.
    /// </summary>
    void ClearParagraph();
}
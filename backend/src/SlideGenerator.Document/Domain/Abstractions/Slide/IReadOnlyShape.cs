/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyShape.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a read-only view of a shape on a PowerPoint slide.
/// </summary>
public interface IReadOnlyShape
{
    /// <summary>
    ///     Gets the name of the shape.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the text displayed within the shape, if any.
    /// </summary>
    string DisplayText { get; }

    /// <summary>
    ///     Gets the bounding box of the shape.
    /// </summary>
    RectangleF Bounds { get; }

    /// <summary>
    ///     Gets the raw image data of the shape if it is a picture; otherwise, null.
    /// </summary>
    byte[]? ImageData { get; }

    /// <summary>
    ///     Gets the collection of paragraphs contained within the shape.
    /// </summary>
    IEnumerable<IReadOnlyParagraph> Paragraph { get; }

    /// <summary>
    ///     Gets the total number of paragraphs in the shape.
    /// </summary>
    int ParagraphsCount { get; }
}
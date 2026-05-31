/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlySlide.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a read-only view of a single slide in a presentation.
/// </summary>
public interface IReadOnlySlide
{
    /// <summary>
    ///     Gets the 1-based slide number.
    /// </summary>
    int Number { get; }

    /// <summary>
    ///     Gets the collection of shapes on the slide.
    /// </summary>
    IEnumerable<IReadOnlyShape> Shapes { get; }

    /// <summary>
    ///     Gets the total number of shapes on the slide.
    /// </summary>
    int ShapesCount { get; }

    /// <summary>
    ///     Gets a preview image of the slide as a byte array.
    /// </summary>
    /// <returns>A byte array containing the slide preview image.</returns>
    byte[] GetPreview();
}
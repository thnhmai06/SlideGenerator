/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ISlide.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a slide in a PowerPoint presentation that can be modified.
/// </summary>
public interface ISlide : IReadOnlySlide
{
    /// <summary>
    ///     Gets the collection of shapes on the slide.
    /// </summary>
    new IEnumerable<IShape> Shapes { get; }

    /// <inheritdoc />
    IEnumerable<IReadOnlyShape> IReadOnlySlide.Shapes => Shapes;
}
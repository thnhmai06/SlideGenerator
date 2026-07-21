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

using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a read-only view of a single slide in a presentation.
/// </summary>
public interface IReadOnlySlide : IEquatable<IReadOnlySlide>
{
    /// <summary>
    ///     Gets the collection of shapes on the slide.
    /// </summary>
    IEnumerable<IReadOnlyShape> Shapes { get; }
    
    /// <summary>
    ///     Gets the identifier of the slide.
    /// </summary>
    SlideIdentifier Identifier { get; }

    /// <summary>
    ///     Gets a preview image of the slide as a byte array.
    /// </summary>
    /// <returns>A byte array containing the slide preview image in PNG format.</returns>
    byte[] GetPreview();

    /// <summary>
    ///     Creates an independent writable copy of this slide, detached from its source presentation.
    /// </summary>
    /// <returns>A new <see cref="ISlide" /> containing the same content as this slide.</returns>
    ISlide Clone();
}
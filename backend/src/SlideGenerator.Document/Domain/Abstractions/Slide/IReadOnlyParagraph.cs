/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyParagraph.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a read-only view of a paragraph in a shape.
/// </summary>
public interface IReadOnlyParagraph
{
    /// <summary>
    ///     Gets the collection of text parts within the paragraph.
    /// </summary>
    IEnumerable<IReadOnlyTextPart> TextParts { get; }

    /// <summary>
    ///     Gets the total number of text parts in the paragraph.
    /// </summary>
    int TextPartsCount { get; }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IParagraph.cs
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
/// Represents a paragraph in a shape that can be modified.
/// </summary>
public interface IParagraph : IReadOnlyParagraph
{
    /// <summary>
    /// Gets the collection of text parts within the paragraph.
    /// </summary>
    new IEnumerable<ITextPart> TextParts { get; }

    /// <summary>
    /// Adds a text part to the end of the paragraph.
    /// </summary>
    /// <param name="textPart">The text part to add.</param>
    /// <returns>The added text part.</returns>
    ITextPart AddTextPart(ITextPart textPart);

    /// <summary>
    /// Removes the text part at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the text part to remove.</param>
    void RemoveAt(int index);

    /// <inheritdoc />
    IEnumerable<IReadOnlyTextPart> IReadOnlyParagraph.TextParts => TextParts;
}






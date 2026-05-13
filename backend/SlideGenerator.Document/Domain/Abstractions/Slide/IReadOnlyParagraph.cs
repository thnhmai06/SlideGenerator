/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyParagraph.cs
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
/// Represents a read-only view of a paragraph in a shape.
/// </summary>
public interface IReadOnlyParagraph
{
    /// <summary>
    /// Gets the collection of text parts within the paragraph.
    /// </summary>
    IEnumerable<IReadOnlyTextPart> TextParts { get; }

    /// <summary>
    /// Gets the total number of text parts in the paragraph.
    /// </summary>
    int TextPartsCount { get; }
}






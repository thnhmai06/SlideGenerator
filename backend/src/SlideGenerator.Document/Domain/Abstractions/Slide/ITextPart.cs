/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ITextPart.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a part of text within a paragraph that can be modified.
/// </summary>
public interface ITextPart : IReadOnlyTextPart
{
    /// <summary>
    ///     Gets or sets the text content of this part.
    /// </summary>
    new string Text { get; set; }

    /// <inheritdoc />
    string IReadOnlyTextPart.Text => Text;
}
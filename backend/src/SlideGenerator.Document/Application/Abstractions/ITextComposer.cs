/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ITextComposer.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Abstractions.Slide;

namespace SlideGenerator.Document.Application.Abstractions;

/// <summary>
///     Renders template placeholders across all paragraphs of a shape while preserving
///     per-TextPart formatting through coverage ratio distribution.
///     Supports cross-paragraph tags and loop expansion.
/// </summary>
public interface ITextComposer
{
    /// <summary>
    ///     Renders all template placeholders in the given shape, replacing them with values
    ///     from <paramref name="resolvedValue" /> while preserving the formatting of each text part.
    /// </summary>
    /// <param name="shape">The shape whose paragraphs will be processed.</param>
    /// <param name="resolvedValue">A dictionary mapping placeholder names to their resolved string values.</param>
    void Compose(IShape shape, IReadOnlyDictionary<string, string> resolvedValue);
}
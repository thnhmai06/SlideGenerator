/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ITextComposer.cs
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
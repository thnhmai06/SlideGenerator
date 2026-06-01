/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SlideIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Models.Slide;

/// <summary>
///     Uniquely identifies a specific slide within a PowerPoint presentation.
/// </summary>
/// <param name="PresentationPath">The path to the presentation.</param>
/// <param name="SlideIndex">The 1-based index of the slide.</param>
/// <param name="PresentationPassword">Optional password for the presentation.</param>
public record SlideIdentifier(string PresentationPath, int SlideIndex, string? PresentationPassword = null)
    : PresentationIdentifier(PresentationPath, PresentationPassword)
{
    /// <summary>
    ///     Gets the 1-based index of the slide. Guaranteed to be at least 1.
    /// </summary>
    public int SlideIndex { get; init; } = Math.Max(1, SlideIndex);
}
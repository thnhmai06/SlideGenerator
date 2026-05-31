/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IReadOnlyPresentation.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Document.Domain.Abstractions.Slide;

/// <summary>
///     Represents a read-only view of a PowerPoint presentation.
/// </summary>
public interface IReadOnlyPresentation : IDisposable
{
    /// <summary>
    ///     Gets the collection of slides in the presentation.
    /// </summary>
    IEnumerable<IReadOnlySlide> Slides { get; }

    /// <summary>
    ///     Gets the total number of slides in the presentation.
    /// </summary>
    int SlidesCount { get; }
}
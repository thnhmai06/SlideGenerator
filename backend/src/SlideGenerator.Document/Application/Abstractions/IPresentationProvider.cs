/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IPresentationProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Document.Application.Abstractions;

/// <summary>
///     Defines the contract for opening PowerPoint presentations.
///     Hides the Syncfusion <c>IPresentation</c> lifecycle from callers.
/// </summary>
public interface IPresentationProvider
{
    /// <summary>
    ///     Opens a presentation identified by <paramref name="identifier" /> in <b>read-write</b> mode.
    /// </summary>
    /// <param name="identifier">The presentation to open.</param>
    /// <returns>A handle wrapping the opened presentation.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the presentation file does not exist.</exception>
    IPresentation OpenPresentation(PresentationIdentifier identifier);

    /// <summary>
    ///     Opens a presentation identified by <paramref name="identifier" /> in <b>read</b> mode.
    /// </summary>
    /// <param name="identifier">The presentation to open.</param>
    /// <returns>A handle wrapping the opened presentation.</returns>
    /// <exception cref="System.IO.FileNotFoundException">If the presentation file does not exist.</exception>
    IReadOnlyPresentation OpenPresentationReadOnly(PresentationIdentifier identifier);
}
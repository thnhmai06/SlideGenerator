/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: IPresentationProvider.cs
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
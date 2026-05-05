/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
 * File: SheetTask.cs
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

using SlideGenerator.Document.Sheet.Models;
using SlideGenerator.Document.Slide.Models;
using SlideGenerator.Pipeline.Generating.Models;

namespace SlideGenerator.Pipeline.Generating.Workflows.Models;

/// <summary>
///     Represents a worksheet that has been validated and assigned an output path.
/// </summary>
public sealed class SheetTask(
    SheetIdentifier identifier,
    SlideIdentifier templateSlide,
    MapNode mapNode,
    PresentationIdentifier outputIdentifier)
{
    /// <summary>Gets the unique identifier for the source worksheet.</summary>
    public SheetIdentifier Identifier { get; } = identifier;

    /// <summary>Gets the identifier for the slide to be used as a template for this sheet.</summary>
    public SlideIdentifier TemplateSlide { get; } = templateSlide;

    /// <summary>Gets the mapping configuration node associated with this sheet.</summary>
    public MapNode MapNode { get; } = mapNode;

    /// <summary>Gets the final output identifier for the generated presentation corresponding to this sheet.</summary>
    public PresentationIdentifier OutputIdentifier { get; } = outputIdentifier;
}
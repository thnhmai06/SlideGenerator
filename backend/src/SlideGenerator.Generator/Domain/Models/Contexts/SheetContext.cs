/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: SheetContext.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Summarization.Domain.Models.Recipes;

namespace SlideGenerator.Generator.Domain.Models.Contexts;

/// <summary>
///     Represents a worksheet that has been validated and assigned an output path.
/// </summary>
public sealed class SheetContext(
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
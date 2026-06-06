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
using SlideGenerator.Recipe.Domain.Models.Graphs;

namespace SlideGenerator.Generator.Domain.Models.Contexts;

/// <summary>
///     Represents a worksheet that has been validated and assigned an output path.
/// </summary>
public sealed class SheetContext(
    WorkbookIdentifier workbook,
    WorksheetNode worksheetNode,
    SlideNode slideNode,
    MapNode mapNode,
    PresentationIdentifier templatePresentation,
    PresentationIdentifier outputIdentifier)
{
    /// <summary>Gets the workbook that owns the source worksheet.</summary>
    public WorkbookIdentifier Workbook { get; } = workbook;

    /// <summary>Gets the source worksheet node.</summary>
    public WorksheetNode WorksheetNode { get; } = worksheetNode;

    /// <summary>Gets the target slide node.</summary>
    public SlideNode SlideNode { get; } = slideNode;

    /// <summary>Gets the mapping configuration node associated with this worksheet.</summary>
    public MapNode MapNode { get; } = mapNode;

    /// <summary>Gets the source presentation template to copy slides from.</summary>
    public PresentationIdentifier TemplatePresentation { get; } = templatePresentation;

    /// <summary>Gets the final output identifier for the generated presentation corresponding to this worksheet.</summary>
    public PresentationIdentifier OutputIdentifier { get; } = outputIdentifier;
}
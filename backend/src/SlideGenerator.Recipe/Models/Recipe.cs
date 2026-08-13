/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Recipe.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;

namespace SlideGenerator.Recipe.Models;

/// <summary>
///     The root structure of a recipe: a flat list of <see cref="Mapping" />, each describing how
///     data from one or more worksheets flows into a single template slide.
/// </summary>
/// <param name="Mappings">Every source-to-template mapping that makes up this recipe.</param>
public sealed record Recipe(IReadOnlyList<Mapping> Mappings)
{
    /// <summary>
    ///     Extracts all unique workbook and presentation file paths referenced by this recipe.
    /// </summary>
    public IReadOnlySet<string> GetReferencedFiles()
    {
        return Mappings
            .SelectMany(m => m.Sources
                .Select(s => s.Workbook.BookPath)
                .Append(m.Template.Presentation.PresentationPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
///     Describes how data from one or more <see cref="WorksheetSource" /> flows into a single
///     template slide, via text and image instructions.
/// </summary>
/// <param name="Sources">Worksheets feeding this mapping.</param>
/// <param name="Template">Identifies the template slide this mapping renders into.</param>
/// <param name="TextInstructions">Rules for mapping worksheet columns to slide text placeholders.</param>
/// <param name="ImageInstructions">Rules for mapping worksheet columns to slide image shapes.</param>
public sealed record Mapping(
    IReadOnlyList<WorksheetSource> Sources,
    PresentationSource Template,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions);

/// <summary>
///     Identifies one worksheet as a data source, with optional column/row filtering.
/// </summary>
/// <param name="Workbook">Identifies the workbook file.</param>
/// <param name="Worksheet">Identifies the worksheet by name within the workbook.</param>
/// <param name="UsedColumns">
///     Column headers visible to downstream instructions. <see langword="null" /> means all columns are used.
/// </param>
/// <param name="RowFilter">
///     Row-selection strategy for this worksheet. <see langword="null" /> means all rows participate.
/// </param>
public sealed record WorksheetSource(
    WorkbookIdentifier Workbook,
    WorksheetIdentifier Worksheet,
    IReadOnlySet<ColumnIdentifier>? UsedColumns = null,
    RowFilter? RowFilter = null);

/// <summary>
///     Identifies one template slide as a render target: the presentation file that hosts it
///     plus the slide within that presentation.
/// </summary>
/// <param name="Presentation">Identifies the presentation file that hosts the template slide.</param>
/// <param name="Slide">Identifies the template slide within <see cref="Presentation" />.</param>
public sealed record PresentationSource(
    PresentationIdentifier Presentation,
    SlideIdentifier Slide);

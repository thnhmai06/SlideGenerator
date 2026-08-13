/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobSpecification.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Generator.Job.Models;

/// <summary>
///     Represents a single slide-generation unit: one worksheet mapped to one template slide, with every
///     value already resolved from the recipe at spawn time (see <c>Service.BuildJobs</c>) — no id needs
///     to be looked back up against the recipe once a job has started, so a job survives independently of
///     whether the originating recipe still exists.
/// </summary>
/// <param name="WorkbookPath">Absolute path to the source workbook.</param>
/// <param name="WorksheetName">Name of the source worksheet within the workbook.</param>
/// <param name="UsedColumns">Column headers visible to instructions, or <see langword="null" /> for all columns.</param>
/// <param name="RowFilter">Row-selection strategy, or <see langword="null" /> for all rows.</param>
/// <param name="TemplatePresentationPath">Absolute path to the presentation hosting the template slide.</param>
/// <param name="TemplateSlideIndex">1-based index of the template slide within <see cref="TemplatePresentationPath" />.</param>
/// <param name="TextInstructions">Rules for mapping worksheet columns to slide text placeholders.</param>
/// <param name="ImageInstructions">Rules for mapping worksheet columns to slide image shapes.</param>
/// <param name="OutputPath">Absolute path to the output presentation file for this job.</param>
public sealed record JobSpecification(
    string WorkbookPath, string WorksheetName,
    IReadOnlySet<ColumnIdentifier>? UsedColumns,
    RowFilter? RowFilter,
    string TemplatePresentationPath, int TemplateSlideIndex,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions,
    string OutputPath);

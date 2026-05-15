/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: ISummarizationService.cs
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

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Summarization.Domain.Models.Recipes;
using SlideGenerator.Summarization.Domain.Models.Sheet;
using SlideGenerator.Summarization.Domain.Models.Slide;

namespace SlideGenerator.Summarization.Application.Abstractions;

/// <summary>
///     Provides methods to analyze and summarize the structure of Excel workbooks and PowerPoint presentations.
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    ///     Analyzes an Excel workbook to extract sheet names, row counts, and optional data previews.
    /// </summary>
    /// <param name="identifier">The workbook identifier containing path and optional password.</param>
    /// <param name="getPreview">Whether to include data row previews in the result.</param>
    /// <returns>A summary of the workbook structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the workbook path is invalid.</exception>
    Task<WorkbookSummary> SummarizeWorkbookAsync(BookIdentifier identifier, bool getPreview = true);

    /// <summary>
    ///     Analyzes a PowerPoint presentation to identify slides, text placeholders, and image-compatible shapes.
    /// </summary>
    /// <param name="identifier">The presentation identifier containing a path and optional password.</param>
    /// <param name="getPreview">Whether to include slide thumbnail previews in the result.</param>
    /// <returns>A summary of the presentation structure.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the presentation path is invalid.</exception>
    Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier, bool getPreview = true);

    /// <summary>
    ///     Parses a ReactFlow recipe JSON string into a <see cref="RecipeSummary" />.
    /// </summary>
    /// <param name="recipe">The ReactFlow graph JSON string representing the recipe.</param>
    /// <returns>The summarized recipe configuration.</returns>
    /// <remarks>TODO: Not yet implemented. Requires ReactFlow JSON schema definition.</remarks>
    RecipeSummary SummarizeRecipe(string recipe);
}
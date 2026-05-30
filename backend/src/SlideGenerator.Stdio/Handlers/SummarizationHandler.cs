/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: SummarizationHandler.cs
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
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Summarization.Application.Abstractions;
using SlideGenerator.Summarization.Domain.Models.Recipes;
using SlideGenerator.Summarization.Domain.Models.Sheet;
using SlideGenerator.Summarization.Domain.Models.Slide;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>summarization.*</c> JSON-RPC methods for inspecting Excel workbooks,
///     PowerPoint presentations, and recipe graphs before a generation job is started.
/// </summary>
public sealed class SummarizationHandler(
    ISummarizationService summarizationService,
    IRecipeRepository recipeRepository)
{
    /// <summary>
    ///     Summarizes an Excel workbook and returns its structure, including worksheet names,
    ///     row counts, and an optional data preview.
    /// </summary>
    /// <param name="identifier">The workbook identifier containing path and optional password.</param>
    /// <param name="getPreview">Whether to include data row previews in the result.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="WorkbookSummary" /> describing the workbook structure.</returns>
    public Task<WorkbookSummary> SummarizeWorkbookAsync(BookIdentifier identifier, bool getPreview,
        CancellationToken ct)
    {
        return summarizationService.SummarizeWorkbookAsync(identifier, getPreview);
    }

    /// <summary>
    ///     Summarizes a PowerPoint presentation and returns its structure, including slide placeholders,
    ///     image shape names and bounds, and optional slide thumbnail previews.
    /// </summary>
    /// <param name="identifier">The presentation identifier containing path and optional password.</param>
    /// <param name="getPreview">Whether to include slide thumbnail previews in the result.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="PresentationSummary" /> describing the presentation structure.</returns>
    public Task<PresentationSummary> SummarizePresentationAsync(PresentationIdentifier identifier, bool getPreview,
        CancellationToken ct)
    {
        return summarizationService.SummarizePresentationAsync(identifier, getPreview);
    }

    /// <summary>
    ///     Parses a ReactFlow recipe JSON string into a <see cref="RecipeSummary" />.
    /// </summary>
    /// <param name="recipe">The ReactFlow graph JSON string representing the recipe.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The summarized recipe configuration.</returns>
    /// <remarks>Not yet implemented — blocked on ReactFlow JSON schema definition.</remarks>
    public Task<RecipeSummary> SummarizeRecipeAsync(string recipe, CancellationToken ct)
    {
        return Task.FromResult(summarizationService.SummarizeRecipe(recipe));
    }

    /// <summary>
    ///     Fetches a recipe from the database by ID, then parses its JSON into a <see cref="RecipeSummary" />.
    /// </summary>
    /// <param name="id">The recipe database identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The summarized recipe configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given ID exists.</exception>
    /// <remarks>Not yet implemented — blocked on ReactFlow JSON schema definition.</remarks>
    public async Task<RecipeSummary> SummarizeRecipeByIdAsync(int id, CancellationToken ct)
    {
        var entry = await recipeRepository.GetByIdAsync(id, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Recipe {id} not found.");
        return summarizationService.SummarizeRecipe(entry.Recipe ?? string.Empty);
    }
}
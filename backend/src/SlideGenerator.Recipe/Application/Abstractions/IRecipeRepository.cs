/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: IRecipeRepository.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Models.Summary;

namespace SlideGenerator.Recipe.Application.Abstractions;

/// <summary>
///     Provides persistent storage for <see cref="RecipeEntry" /> configurations.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    ///     Inserts a new recipe row and returns its metadata.
    /// </summary>
    /// <param name="input">The recipe input containing the name and graph data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="IRecipeMetadata" /> of the newly inserted row.</returns>
    Task<IRecipeMetadata> AddAsync(RecipeInput input, CancellationToken ct = default);

    /// <summary>
    ///     Retrieves a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="RecipeEntry" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given id exists.</exception>
    Task<RecipeEntry> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    ///     Returns metadata for all stored recipe entries ordered by the most recently updated.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<IRecipeMetadata>> ListAsync(CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing recipe entry.
    /// </summary>
    /// <param name="id">The database-generated id of the recipe to update.</param>
    /// <param name="input">The new name and graph data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="IRecipeMetadata" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given id exists.</exception>
    Task<IRecipeMetadata> UpdateAsync(int id, RecipeInput input, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true" /> if a row was deleted; <see langword="false" /> if the id was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    ///     Exports a stored recipe as a <c>*.recipe</c> package file.
    /// </summary>
    /// <param name="id">The id of the recipe to export.</param>
    /// <param name="outputPath">The full path to write the output file.</param>
    /// <param name="password">Optional password. Pass <see langword="null" /> for no encryption.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExportAsync(int id, string outputPath, string? password, CancellationToken ct = default);

    /// <summary>
    ///     Imports a <c>*.recipe</c> package file, extracts its resources, and stores the recipe in the database.
    /// </summary>
    /// <param name="filePath">The full path to the <c>*.recipe</c> file.</param>
    /// <param name="password">Optional password. Pass <see langword="null" /> if the archive is not encrypted.</param>
    /// <param name="workbooksDirectory">The directory into which workbook files will be extracted.</param>
    /// <param name="presentationsDirectory">The directory into which presentation files will be extracted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The database-generated id of the newly imported recipe.</returns>
    Task<IRecipeMetadata> ImportAsync(
        string filePath, string? password,
        (string Workbooks, string Presentations) saveFolders,
        CancellationToken ct = default);

    /// <summary>
    ///     Fetches a recipe from the database by id and parses its graph into a <see cref="RecipeSummary" />.
    /// </summary>
    /// <param name="id">The database-generated id of the recipe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parsed <see cref="RecipeSummary" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given id exists.</exception>
    Task<RecipeSummary> SummarizeAsync(int id, CancellationToken ct = default);

    /// <summary>
    ///     Parses recipe graph into a <see cref="RecipeSummary" />.
    /// </summary>
    /// <param name="graph">The recipe graph data.</param>
    /// <returns>The parsed <see cref="RecipeSummary" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no recipe with the given id exists.</exception>
    static abstract RecipeSummary Summarize(string graph);
}

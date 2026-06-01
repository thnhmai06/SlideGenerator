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

namespace SlideGenerator.Recipe.Application.Abstractions;

/// <summary>
///     Provides persistent storage for <see cref="RecipeEntry" /> configurations.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    ///     Inserts a new recipe row and returns its generated id.
    /// </summary>
    /// <param name="displayName">Optional human-readable display name.</param>
    /// <param name="recipe">Optional ReactFlow graph JSON string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The database-generated id of the new row.</returns>
    Task<int> AddAsync(string? displayName, string? recipe, CancellationToken ct = default);

    /// <summary>
    ///     Retrieves a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="RecipeEntry" />, or <see langword="null" /> if not found.</returns>
    Task<RecipeEntry?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    ///     Returns all stored recipe entries ordered by id.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<RecipeEntry>> ListAsync(CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing recipe entry.
    /// </summary>
    /// <param name="id">The database-generated id of the recipe to update.</param>
    /// <param name="displayName">New display name.</param>
    /// <param name="recipe">New ReactFlow JSON, or <see langword="null" /> to clear it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true" /> if a row was updated; <see langword="false" /> if the id was not found.</returns>
    Task<bool> UpdateAsync(int id, string? displayName, string? recipe, CancellationToken ct = default);

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
    /// <param name="recipeId">The id of the recipe to export.</param>
    /// <param name="outputFilePath">The full path to write the output file.</param>
    /// <param name="password">Optional AES-256 password. Pass <see langword="null" /> for no encryption.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExportAsync(int recipeId, string outputFilePath, string? password, CancellationToken ct = default);

    /// <summary>
    ///     Imports a <c>*.recipe</c> package file, extracts its resources, and stores the recipe in the database.
    /// </summary>
    /// <param name="filePath">The full path to the <c>*.recipe</c> file.</param>
    /// <param name="password">Optional AES-256 password. Pass <see langword="null" /> if the archive is not encrypted.</param>
    /// <param name="workbooksDirectory">The directory into which workbook files will be extracted.</param>
    /// <param name="presentationsDirectory">The directory into which presentation files will be extracted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The database-generated id of the newly imported recipe.</returns>
    Task<int> ImportAsync(string filePath, string? password, string workbooksDirectory,
        string presentationsDirectory, CancellationToken ct = default);
}
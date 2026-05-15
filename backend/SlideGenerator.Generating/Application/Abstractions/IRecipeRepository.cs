/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: IRecipeRepository.cs
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

using SlideGenerator.Generating.Domain.Models.Recipes;

namespace SlideGenerator.Generating.Application.Abstractions;

/// <summary>
///     Provides persistent storage for <see cref="Recipe" /> configurations.
///     Identity is determined by recipe content: two entries with identical content share the same row and id.
///     <c>Name</c> and <c>DisplayString</c> are display-only metadata, not identity fields.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    ///     Inserts a new recipe row and returns its generated id.
    /// </summary>
    /// <param name="recipe">The recipe configuration to store.</param>
    /// <param name="displayName">Optional human-readable display name.</param>
    /// <param name="flowData">Optional ReactFlow graph JSON for UI rendering.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The database-generated id of the new row.</returns>
    Task<int> AddAsync(Recipe recipe, string? displayName, string? flowData, CancellationToken ct = default);

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
    ///     When <paramref name="recipe" /> is provided, the stored recipe content is replaced;
    ///     if the new content already exists in another row, the update is rejected and returns <see langword="false" />.
    /// </summary>
    /// <param name="id">The database-generated id of the recipe to update.</param>
    /// <param name="displayName">New display name.</param>
    /// <param name="flowData">New ReactFlow graph JSON, or <see langword="null" /> to clear it.</param>
    /// <param name="recipe">New recipe content, or <see langword="null" /> to leave content unchanged.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true" /> if a row was updated; <see langword="false" /> if the id was not found or content conflicts.</returns>
    Task<bool> UpdateAsync(int id, string? displayName, string? flowData, Recipe? recipe = null, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true" /> if a row was deleted; <see langword="false" /> if the id was not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
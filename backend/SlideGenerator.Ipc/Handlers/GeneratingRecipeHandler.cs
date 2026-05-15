/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: GeneratingRecipeHandler.cs
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

using SlideGenerator.Generating.Application.Abstractions;
using SlideGenerator.Generating.Domain.Models.Recipes;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>generating.recipe.*</c> JSON-RPC methods.
///     Provides CRUD access to stored <see cref="RecipeEntry" /> records.
/// </summary>
public sealed class GeneratingRecipeHandler(IRecipeRepository recipeRepository)
{
    /// <summary>
    ///     Returns all stored recipe entries ordered by id.
    /// </summary>
    public Task<IReadOnlyList<RecipeEntry>> ListAsync(CancellationToken ct)
    {
        return recipeRepository.ListAsync(ct);
    }

    /// <summary>
    ///     Returns a single recipe entry by its id, or <see langword="null" /> if not found.
    /// </summary>
    public Task<RecipeEntry?> QueryAsync(int id, CancellationToken ct)
    {
        return recipeRepository.GetByIdAsync(id, ct);
    }

    /// <summary>
    ///     Inserts a new recipe row and returns its generated id.
    /// </summary>
    public Task<int> AddAsync(Recipe recipe, string? displayName, string? flowData, CancellationToken ct)
    {
        return recipeRepository.AddAsync(recipe, displayName, flowData, ct);
    }

    /// <summary>
    ///     Updates an existing recipe entry. Pass <paramref name="recipe" /> to also replace the recipe content.
    /// </summary>
    /// <returns><see langword="true" /> if updated; <see langword="false" /> if the id was not found or content conflicts.</returns>
    public Task<bool> UpdateAsync(int id, string? displayName, string? flowData, Recipe? recipe, CancellationToken ct)
    {
        return recipeRepository.UpdateAsync(id, displayName, flowData, recipe, ct);
    }

    /// <summary>
    ///     Permanently deletes a recipe entry by its id.
    /// </summary>
    /// <returns><see langword="true" /> if deleted; <see langword="false" /> if the id was not found.</returns>
    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        return recipeRepository.DeleteAsync(id, ct);
    }
}

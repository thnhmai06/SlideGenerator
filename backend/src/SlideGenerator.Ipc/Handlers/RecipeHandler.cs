/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: RecipeHandler.cs
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

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Domain.Models;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>recipe.*</c> JSON-RPC methods.
///     Provides CRUD access and package export/import for <see cref="RecipeEntry" /> records.
/// </summary>
public sealed class RecipeHandler(IRecipeRepository recipeRepository, IGeneratingService generatingService)
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
    public Task<int> AddAsync(string? displayName, string? recipe, CancellationToken ct)
    {
        return recipeRepository.AddAsync(displayName, recipe, ct);
    }

    /// <summary>
    ///     Updates an existing recipe entry.
    /// </summary>
    /// <returns><see langword="true" /> if updated; <see langword="false" /> if the id was not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the recipe is currently in use by an active workflow.</exception>
    public async Task<bool> UpdateAsync(int id, string? displayName, string? recipe, CancellationToken ct)
    {
        if (await generatingService.IsRecipeInUseAsync(id, ct).ConfigureAwait(false))
            throw new InvalidOperationException(
                $"Recipe {id} is currently in use by an active workflow and cannot be modified.");
        return await recipeRepository.UpdateAsync(id, displayName, recipe, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Permanently deletes a recipe entry by its id.
    /// </summary>
    /// <returns><see langword="true" /> if deleted; <see langword="false" /> if the id was not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the recipe is currently in use by an active workflow.</exception>
    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        if (await generatingService.IsRecipeInUseAsync(id, ct).ConfigureAwait(false))
            throw new InvalidOperationException(
                $"Recipe {id} is currently in use by an active workflow and cannot be deleted.");
        return await recipeRepository.DeleteAsync(id, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Exports a stored recipe as a <c>*.recipe</c> package file.
    /// </summary>
    public Task ExportAsync(int recipeId, string outputFilePath, string? password, CancellationToken ct)
    {
        return recipeRepository.ExportAsync(recipeId, outputFilePath, password, ct);
    }

    /// <summary>
    ///     Imports a <c>*.recipe</c> package file and returns the id of the newly stored recipe.
    /// </summary>
    public Task<int> ImportAsync(string filePath, string? password, string workbooksDirectory,
        string presentationsDirectory, CancellationToken ct)
    {
        return recipeRepository.ImportAsync(filePath, password, workbooksDirectory, presentationsDirectory, ct);
    }
}
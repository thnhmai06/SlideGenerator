/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: RecipeHandler.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Models.Graphs;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>recipe.*</c> JSON-RPC methods.
///     Provides CRUD access and package export/import for <see cref="RecipeEntry" /> records.
/// </summary>
public sealed class RecipeHandler(IRecipeRepository recipeRepository, IGeneratingService generatingService)
{
    /// <summary>
    ///     Returns metadata for all stored recipe entries, ordered by the most recently updated.
    /// </summary>
    public Task<IReadOnlyList<IRecipeMetadata>> ListAsync(CancellationToken ct)
    {
        return recipeRepository.ListAsync(ct);
    }

    /// <summary>
    ///     Returns a single recipe entry by its id, or <see langword="null" /> if not found.
    /// </summary>
    public Task<RecipeEntry?> QueryAsync(int id, CancellationToken ct)
    {
        return recipeRepository.GetAsync(id, ct);
    }

    /// <summary>
    ///     Inserts a new recipe row and returns its metadata.
    /// </summary>
    /// <param name="displayName">Human-readable name.</param>
    /// <param name="graph">The recipe graph.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<IRecipeMetadata> AddAsync(string displayName, RecipeGraph graph, CancellationToken ct)
    {
        return recipeRepository.AddAsync(new RecipeInput(displayName, graph), ct);
    }

    /// <summary>
    ///     Updates an existing recipe entry.
    /// </summary>
    /// <param name="id">The recipe id.</param>
    /// <param name="displayName">New name.</param>
    /// <param name="graph">New recipe graph.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="IRecipeMetadata" />.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the recipe is not found or is currently in use by an active
    ///     workflow.
    /// </exception>
    public async Task<IRecipeMetadata> UpdateAsync(int id, string displayName, RecipeGraph graph, CancellationToken ct)
    {
        if (await generatingService.IsRecipeInUseAsync(id, ct).ConfigureAwait(false))
            throw new InvalidOperationException(
                $"Recipe {id} is currently in use by an active workflow and cannot be modified.");
        return await recipeRepository.UpdateAsync(id, new RecipeInput(displayName, graph), ct).ConfigureAwait(false);
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
    public Task<IRecipeMetadata> ImportAsync(string filePath, string? password, string workbooksDirectory,
        string presentationsDirectory, CancellationToken ct)
    {
        return recipeRepository.ImportAsync(filePath, password, (workbooksDirectory, presentationsDirectory), ct);
    }
}
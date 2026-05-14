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
using SlideGenerator.Generating.Domain.Models;
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
    ///     Returns the id of the existing recipe if one with the same content already exists,
    ///     or inserts a new row and returns its generated id.
    /// </summary>
    /// <param name="recipe">The recipe whose content determines identity.</param>
    /// <param name="name">Display name — stored on the first insert, ignored on further calls with the same content.</param>
    /// <param name="flowData">Optional ReactFlow graph JSON for UI rendering.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The id of the matching or newly created recipe row.</returns>
    Task<int> GetOrAddAsync(Recipe recipe, string name, string? flowData, CancellationToken ct = default);

    /// <summary>
    ///     Retrieves a recipe entry by its id.
    /// </summary>
    /// <param name="id">The database-generated id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="RecipeEntry" />, or <see langword="null" /> if not found.</returns>
    Task<RecipeEntry?> GetByIdAsync(int id, CancellationToken ct = default);
}

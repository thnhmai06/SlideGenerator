/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeEntry.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Models;

/// <summary>
///     Represents a persisted recipe entry containing its storage metadata and ReactFlow JSON.
/// </summary>
/// <param name="Id">The database-generated identifier.</param>
/// <param name="DisplayName">Optional human-readable display name of the recipe.</param>
/// <param name="Recipe">Optional ReactFlow graph JSON string.</param>
/// <param name="CreatedTimestamp">UTC timestamp when the entry was first created.</param>
/// <param name="UpdatedTimestamp">UTC timestamp when the entry was last updated.</param>
public record RecipeEntry(
    int Id,
    string? DisplayName,
    string? Recipe,
    DateTimeOffset CreatedTimestamp,
    DateTimeOffset UpdatedTimestamp);
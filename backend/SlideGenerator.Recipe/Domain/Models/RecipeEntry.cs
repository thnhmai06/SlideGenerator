/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeEntry.cs
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
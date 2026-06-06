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

using SlideGenerator.Recipe.Domain.Models.Graphs;

namespace SlideGenerator.Recipe.Domain.Models;

/// <summary>
///     The mutable user-visible properties of a recipe, used for create/update operations.
/// </summary>
/// <param name="Name">Human-readable display name of the recipe.</param>
/// <param name="Graph">The recipe graph.</param>
public record RecipeInput(string Name, RecipeGraph Graph);

/// <summary>
///     Lightweight projection containing the identity and metadata of a recipe entry (no graph payload).
/// </summary>
public interface IRecipeMetadata
{
    /// <summary>The database-generated identifier.</summary>
    int Id { get; }

    /// <summary>Human-readable display name of the recipe.</summary>
    string Name { get; }

    /// <summary>UTC timestamp when the entry was first created.</summary>
    DateTimeOffset CreatedTimestamp { get; }

    /// <summary>UTC timestamp when the entry was last updated.</summary>
    DateTimeOffset UpdatedTimestamp { get; }
}

/// <summary>
///     Represents a persisted recipe entry containing its storage metadata and graph data.
///     Implements <see cref="IRecipeMetadata" />.
/// </summary>
/// <param name="Id">The database-generated identifier.</param>
/// <param name="Name">Human-readable display name of the recipe.</param>
/// <param name="Graph">The deserialized recipe graph.</param>
/// <param name="CreatedTimestamp">UTC timestamp when the entry was first created.</param>
/// <param name="UpdatedTimestamp">UTC timestamp when the entry was last updated.</param>
public record RecipeEntry(
    int Id,
    string Name,
    RecipeGraph Graph,
    DateTimeOffset CreatedTimestamp,
    DateTimeOffset UpdatedTimestamp)
    : RecipeInput(Name, Graph), IRecipeMetadata;
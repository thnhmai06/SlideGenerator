/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: RecipeSummary.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Summarization.Domain.Models.Recipes;

/// <summary>
///     Represents the summarized configuration derived from a ReactFlow recipe JSON.
///     A recipe summary consists of multiple mapping nodes that define how various data sources are merged into slides.
/// </summary>
/// <param name="Nodes">The list of mapping nodes that form the recipe summary.</param>
public record RecipeSummary(IReadOnlyList<MapNode> Nodes);
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Summarization
 * File: RecipeSummary.cs
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

namespace SlideGenerator.Summarization.Domain.Models.Recipes;

/// <summary>
///     Represents the summarized configuration derived from a ReactFlow recipe JSON.
///     A recipe summary consists of multiple mapping nodes that define how various data sources are merged into slides.
/// </summary>
/// <param name="Nodes">The list of mapping nodes that form the recipe summary.</param>
public record RecipeSummary(IReadOnlyList<MapNode> Nodes);
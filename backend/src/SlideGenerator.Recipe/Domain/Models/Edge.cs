/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Edge.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Models;

/// <summary>
///     A directed data-flow connection between two <see cref="CanvasNode"/> in a recipe graph.
/// </summary>
/// <param name="FromId">The node ID of the source node.</param>
/// <param name="ToId">The node ID of the target node.</param>
public sealed record Edge(
    string FromId,
    string ToId);
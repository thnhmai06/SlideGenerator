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

namespace SlideGenerator.Recipe.Domain.Models.Graphs;

/// <summary>
///     A directed data-flow connection between two nodes in a recipe graph.
///     Valid directions: <see cref="WorksheetNode" /> → <see cref="MapNode" /> → <see cref="SlideNode" />.
///     Parent-child containment (workbook/worksheet, presentation/slide) is expressed via
///     <see cref="WorksheetNode.ParentId" /> and <see cref="SlideNode.ParentId" />, not via edges.
/// </summary>
/// <param name="FromId"><see cref="Node.Id" /> of the source node.</param>
/// <param name="ToId"><see cref="Node.Id" /> of the target node.</param>
public sealed record Edge(
    string FromId,
    string ToId);
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeGraph.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Models.Graphs;

/// <summary>
///     The root structure of a recipe graph, containing all nodes and directed edges.
/// </summary>
/// <remarks>
///     Data-flow edges run <see cref="WorksheetNode" /> → <see cref="MapNode" /> → <see cref="SlideNode" />.
///     Parent-child containment (workbook/worksheet, presentation/slide) is encoded in
///     <see cref="WorksheetNode.ParentId" /> and <see cref="SlideNode.ParentId" />, not in <see cref="Edges" />.
/// </remarks>
/// <param name="Nodes">All nodes in the graph.</param>
/// <param name="Edges">Directed data-flow edges between nodes.</param>
public record RecipeGraph(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Edge> Edges)
{
    /// <summary>
    ///     Extracts all unique file paths referenced by workbook and presentation nodes in the graph.
    /// </summary>
    public (IReadOnlySet<string> Workbooks, IReadOnlySet<string> Presentations) GetReferencedFiles()
    {
        var workbooks = Nodes.OfType<WorkbookNode>()
            .Select(n => n.Workbook.BookPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var presentations = Nodes.OfType<PresentationNode>()
            .Select(n => n.Presentation.PresentationPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (workbooks, presentations);
    }
}
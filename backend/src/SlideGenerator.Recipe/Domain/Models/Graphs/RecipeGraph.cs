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
///     The root structure of a recipe graph, containing all canvas nodes and directed edges.
/// </summary>
/// <remarks>
///     Data-flow edges run <see cref="WorksheetNode" /> → <see cref="MapNode" /> → <see cref="SlideNode" />.
///     Containment (workbook/worksheet, presentation/slide) is expressed by embedding child nodes
///     directly inside their parent: <see cref="WorkbookNode.Sheets" /> and <see cref="PresentationNode.Slides" />.
///     <see cref="ChildNode" /> instances never appear at the top level of <see cref="Nodes" />.
/// </remarks>
/// <param name="Nodes">All top-level <see cref="CanvasNode" /> instances in the graph.</param>
/// <param name="Edges">Directed data-flow edges between nodes.</param>
public record RecipeGraph(
    IReadOnlyList<CanvasNode> Nodes,
    IReadOnlyList<Edge> Edges)
{
    /// <summary>
    ///     Extracts all unique file paths referenced by workbook and presentation nodes in the graph.
    /// </summary>No
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
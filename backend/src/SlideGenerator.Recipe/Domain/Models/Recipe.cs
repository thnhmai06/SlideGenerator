/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Recipe.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Models;

/// <summary>
///     The root structure of a recipe as a graph, containing all <see cref="Node"/> keyed by id
///     and directed <see cref="Edge"/>.
/// </summary>
/// <remarks>
///     Data-flow edges run <see cref="WorksheetNode" /> → <see cref="MapNode" /> → <see cref="SlideNode" />.
///     Containment (workbook/worksheet, presentation/slide) is expressed by id-references:
///     <see cref="WorkbookNode.WorksheetIds" /> and <see cref="PresentationNode.SlideIds" /> list the ids of
///     their child nodes, which are stored as first-class entries in <see cref="Nodes" />.
/// </remarks>
/// <param name="Nodes">All <see cref="Node" /> instances (canvas and child) keyed by their unique id.</param>
/// <param name="Edges">Directed data-flow <see cref="Edge"/> between nodes.</param>
public record Recipe(
    IReadOnlyDictionary<string, Node> Nodes,
    IReadOnlyList<Edge> Edges)
{
    /// <summary>
    ///     Extracts all unique file paths referenced by workbook and presentation nodes in the graph.
    /// </summary>
    public (IReadOnlySet<string> Workbooks, IReadOnlySet<string> Presentations) GetReferencedFiles()
    {
        var workbooks = Nodes.Values.OfType<WorkbookNode>()
            .Select(n => n.Workbook.BookPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var presentations = Nodes.Values.OfType<PresentationNode>()
            .Select(n => n.Presentation.PresentationPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (workbooks, presentations);
    }
}

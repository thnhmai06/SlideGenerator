/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RecipeSummary.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Models.Summary;

/// <summary>
///     Represents the summarized configuration derived from recipe graph data.
///     A recipe summary consists of multiple mapping nodes that define how various data sources are merged into slides.
/// </summary>
/// <param name="Nodes">The list of mapping nodes that form the recipe summary.</param>
public record RecipeSummary(IReadOnlyList<MapNode> Nodes)
{
    /// <summary>
    ///     Extracts all unique file paths (workbooks and presentations) referenced within the mapping nodes.
    /// </summary>
    /// <returns>
    ///     A tuple containing a set of workbook paths and a set of presentation paths.
    /// </returns>
    public (IReadOnlySet<string> Workbooks, IReadOnlySet<string> Presentations) GetReferencedFiles()
    {
        var workbooks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var presentations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in Nodes)
        {
            foreach (var sheet in node.Sheets)
                workbooks.Add(sheet.BookPath);
            presentations.Add(node.Slide.PresentationPath);
        }

        return (workbooks, presentations);
    }
}

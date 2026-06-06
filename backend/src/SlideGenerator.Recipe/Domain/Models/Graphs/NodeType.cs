/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: NodeType.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Recipe.Domain.Models.Graphs;

/// <summary>
///     Discriminates the concrete type of a <see cref="Node" /> within a recipe graph.
/// </summary>
public enum NodeType
{
    /// <summary>Represents an Excel workbook file.</summary>
    Workbook,

    /// <summary>Represents a worksheet that belongs to a <see cref="Workbook" /> node.</summary>
    Worksheet,

    /// <summary>Represents a PowerPoint presentation file.</summary>
    Presentation,

    /// <summary>Represents a slide that belongs to a <see cref="Presentation" /> node.</summary>
    Slide,

    /// <summary>Represents a mapping that connects worksheet sources to a slide target.</summary>
    Map,

    /// <summary>Represents a free-floating annotation on the canvas.</summary>
    Comment
}
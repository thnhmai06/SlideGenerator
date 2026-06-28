/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: Node.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Recipe.Domain.Models.Components;

namespace SlideGenerator.Recipe.Domain.Models.Graphs;

#region Base

/// <summary>
///     Base record for any entity in the recipe graph that carries a unique identity and
///     can serve as an endpoint of a directed <see cref="Edge" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
public abstract record Node(string Id, NodeType Type);

/// <summary>
///     A node that exists independently on the canvas and has its own position.
///     Only <see cref="CanvasNode" /> instances appear in <see cref="RecipeGraph.Nodes" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
/// <param name="Position">Canvas position of the node.</param>
public abstract record CanvasNode(string Id, NodeType Type, Point Position) : Node(Id, Type);

/// <summary>
///     A node embedded inside a <see cref="CanvasNode" /> container — it does not appear
///     at the top level of <see cref="RecipeGraph.Nodes" /> and has no canvas position.
///     It can still be an edge endpoint via its <see cref="Node.Id" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
public abstract record ChildNode(string Id, NodeType Type) : Node(Id, Type);

#endregion

#region Workbooks

/// <summary>
///     A node that references an Excel workbook file.
///     Child <see cref="WorksheetNode" /> instances that participate in the recipe
///     are stored directly in <see cref="Sheets" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Workbook">Identifies the workbook file.</param>
/// <param name="Sheets">Worksheet nodes that participate in this recipe.</param>
public record WorkbookNode(
    string Id,
    Point Position,
    WorkbookIdentifier Workbook,
    IReadOnlyList<WorksheetNode> Sheets)
    : CanvasNode(Id, NodeType.Workbook, Position);

/// <summary>
///     A node that represents one worksheet within a parent <see cref="WorkbookNode" />.
///     Configures which columns and rows are exposed to connected <see cref="MapNode" /> instances.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Worksheet">Identifies the worksheet by name within the parent workbook.</param>
/// <param name="AllowedColumns">
///     Column headers visible to downstream nodes. <see langword="null" /> means all columns are allowed.
/// </param>
/// <param name="RowFilter">
///     Row-selection strategy for this worksheet. <see langword="null" /> means all rows participate.
/// </param>
public record WorksheetNode(
    string Id,
    WorksheetIdentifier Worksheet,
    IReadOnlySet<ColumnIdentifier>? AllowedColumns = null,
    RowFilter? RowFilter = null)
    : ChildNode(Id, NodeType.Worksheet);

#endregion

#region Presentations

/// <summary>
///     A node that references a PowerPoint presentation file.
///     Child <see cref="SlideNode" /> instances that participate in the recipe
///     are stored directly in <see cref="Slides" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Presentation">Identifies the presentation file.</param>
/// <param name="Slides">Slide nodes that participate in this recipe.</param>
public record PresentationNode(
    string Id,
    Point Position,
    PresentationIdentifier Presentation,
    IReadOnlyList<SlideNode> Slides)
    : CanvasNode(Id, NodeType.Presentation, Position);

/// <summary>
///     A node that represents one slide within a parent <see cref="PresentationNode" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Slide">Identifies the slide by 1-based index within the parent presentation.</param>
public record SlideNode(string Id, SlideIdentifier Slide)
    : ChildNode(Id, NodeType.Slide);

#endregion

#region Domain

/// <summary>
///     A node that defines how data flows from one or more <see cref="WorksheetNode" /> sources
///     to a <see cref="SlideNode" /> target. Connections are expressed as directed <see cref="Edge" /> records
///     in the containing <see cref="RecipeGraph" />.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="TextInstructions">Rules for mapping worksheet columns to slide text placeholders.</param>
/// <param name="ImageInstructions">Rules for mapping worksheet columns to slide image shapes.</param>
public record MapNode(
    string Id,
    Point Position,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions)
    : CanvasNode(Id, NodeType.Map, Position);

/// <summary>
///     A free-floating annotation node on the canvas.
/// </summary>
/// <param name="Id">Unique identifier within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Color">Background color of the comment card.</param>
/// <param name="Opacity">Opacity of the comment card, in the range [0, 1].</param>
/// <param name="MarkdownText">Markdown-formatted body text.</param>
public record CommentNode(
    string Id,
    Point Position,
    Color Color,
    float Opacity,
    string MarkdownText)
    : CanvasNode(Id, NodeType.Comment, Position);

#endregion
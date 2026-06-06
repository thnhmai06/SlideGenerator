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

/// <summary>
///     Base record for all nodes in a recipe graph.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
/// <param name="Position">Canvas position of the node.</param>
public abstract record Node(
    string Id,
    NodeType Type,
    Point Position);

/// <summary>
///     A node that references an Excel workbook file.
///     Acts as a container — its child <see cref="WorksheetNode" /> instances are found by matching
///     their <see cref="WorksheetNode.ParentId" /> to this node's <see cref="Node.Id" />.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Workbook">Identifies the workbook file.</param>
public record WorkbookNode(
    string Id,
    Point Position,
    WorkbookIdentifier Workbook) //! DON'T STORE WORKSHEETS HERE
    : Node(Id, NodeType.Workbook, Position);

/// <summary>
///     A node that represents one worksheet within a parent <see cref="WorkbookNode" />.
///     Configures which columns and rows are exposed to connected <see cref="MapNode" /> instances.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="ParentId">
///     <see cref="Node.Id" /> of the <see cref="WorkbookNode" /> that owns this worksheet.
/// </param>
/// <param name="Worksheet">Identifies the worksheet by name within the parent workbook.</param>
/// <param name="AllowedColumns">
///     Column headers visible to downstream nodes. <see langword="null" /> means all columns are allowed.
/// </param>
/// <param name="RowFilter">
///     Row-selection strategy for this worksheet. <see langword="null" /> means all rows participate.
/// </param>
public record WorksheetNode(
    string Id,
    Point Position,
    string ParentId,
    WorksheetIdentifier Worksheet,
    IReadOnlySet<ColumnIdentifier>? AllowedColumns = null,
    RowFilter? RowFilter = null)
    : Node(Id, NodeType.Worksheet, Position);

/// <summary>
///     A node that references a PowerPoint presentation file.
///     Acts as a container — its child <see cref="SlideNode" /> instances are found by matching
///     their <see cref="SlideNode.ParentId" /> to this node's <see cref="Node.Id" />.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Presentation">Identifies the presentation file.</param>
public record PresentationNode(
    string Id,
    Point Position,
    PresentationIdentifier Presentation) //! DON'T STORE SLIDES HERE
    : Node(Id, NodeType.Presentation, Position);

/// <summary>
///     A node that represents one slide within a parent <see cref="PresentationNode" />.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="ParentId">
///     <see cref="Node.Id" /> of the <see cref="PresentationNode" /> that owns this slide.
/// </param>
/// <param name="Slide">Identifies the slide by 1-based index within the parent presentation.</param>
public record SlideNode(
    string Id,
    Point Position,
    string ParentId,
    SlideIdentifier Slide)
    : Node(Id, NodeType.Slide, Position);

/// <summary>
///     A node that defines how data flows from one or more <see cref="WorksheetNode" /> sources
///     to a <see cref="SlideNode" /> target. Connections are expressed as directed <see cref="Edge" /> records
///     in the containing <see cref="RecipeGraph" />.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="TextInstructions">Rules for mapping worksheet columns to slide text placeholders.</param>
/// <param name="ImageInstructions">Rules for mapping worksheet columns to slide image shapes.</param>
public record MapNode(
    string Id,
    Point Position,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions)
    : Node(Id, NodeType.Map, Position);

/// <summary>
///     A free-floating annotation node on the canvas.
/// </summary>
/// <param name="Id">Unique identifier of the node within the graph.</param>
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
    : Node(Id, NodeType.Comment, Position);
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
using SlideGenerator.Document.Slide;
using SlideGenerator.Document.Workbook;
using SlideGenerator.Recipe.Adapters;
using SlideGenerator.Recipe.Models.Components;

namespace SlideGenerator.Recipe.Models;

#region Base

/// <summary>
///     Base record for any entity in the recipe graph that can serve as an endpoint
///     of a directed <see cref="Edge" />.
///     All nodes are keyed by their id in <see cref="Recipe.Nodes" /> — the id is not stored
///     on the record itself (it is always <see cref="string.Empty" /> at rest).
/// </summary>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
[Newtonsoft.Json.JsonConverter(typeof(NodeNewtonsoftConverter))]
public abstract record Node(NodeType Type);

/// <summary>
///     A node that exists independently on the canvas and has its own position.
/// </summary>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
/// <param name="Position">Canvas position of the node.</param>
public abstract record CanvasNode(NodeType Type, Point Position) : Node(Type);

/// <summary>
///     A node owned by a <see cref="CanvasNode" /> container.
/// </summary>
/// <param name="Type">Discriminator indicating the concrete node type.</param>
public abstract record ChildNode(NodeType Type) : Node(Type);

#endregion

#region Workbooks

/// <summary>
///     A node that references an Excel workbook file.
///     The ids of participating <see cref="WorksheetNode" /> instances are listed in <see cref="WorksheetIds" />;
///     their records live in <see cref="Recipe.Nodes" />.
/// </summary>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Workbook">Identifies the workbook file.</param>
/// <param name="WorksheetIds">Ids of <see cref="WorksheetNode" /> entries in <see cref="Recipe.Nodes" />.</param>
public record WorkbookNode(
    Point Position,
    WorkbookIdentifier Workbook,
    [property: Newtonsoft.Json.JsonConverter(typeof(ReadOnlySetNewtonsoftConverter))]
    IReadOnlySet<string> WorksheetIds)
    : CanvasNode(NodeType.Workbook, Position);

/// <summary>
///     A node that represents one worksheet within a parent <see cref="WorkbookNode" />.
///     Its id is the key in <see cref="Recipe.Nodes" />.
/// </summary>
/// <param name="Worksheet">Identifies the worksheet by name within the parent workbook.</param>
/// <param name="UsedColumns">
///     Column headers visible to downstream nodes. <see langword="null" /> means all columns are used.
/// </param>
/// <param name="RowFilter">
///     Row-selection strategy for this worksheet. <see langword="null" /> means all rows participate.
/// </param>
public record WorksheetNode(
    WorksheetIdentifier Worksheet,
    IReadOnlySet<ColumnIdentifier>? UsedColumns = null,
    RowFilter? RowFilter = null)
    : ChildNode(NodeType.Worksheet);

#endregion

#region Presentations

/// <summary>
///     A node that references a PowerPoint presentation file.
///     The ids of participating <see cref="SlideNode" /> instances are listed in <see cref="SlideIds" />;
///     their records live in <see cref="Recipe.Nodes" />.
/// </summary>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Presentation">Identifies the presentation file.</param>
/// <param name="SlideIds">Ids of <see cref="SlideNode" /> entries in <see cref="Recipe.Nodes" />.</param>
public record PresentationNode(
    Point Position,
    PresentationIdentifier Presentation,
    [property: Newtonsoft.Json.JsonConverter(typeof(ReadOnlySetNewtonsoftConverter))]
    IReadOnlySet<string> SlideIds)
    : CanvasNode(NodeType.Presentation, Position);

/// <summary>
///     A node that represents one slide within a parent <see cref="PresentationNode" />.
///     Its id is the key in <see cref="Recipe.Nodes" />.
/// </summary>
/// <param name="Slide">Identifies the slide by 1-based index within the parent presentation.</param>
public record SlideNode(SlideIdentifier Slide)
    : ChildNode(NodeType.Slide);

#endregion

#region Domain

/// <summary>
///     A node that defines how data flows from one or more <see cref="WorksheetNode" /> sources
///     to a <see cref="SlideNode" /> target. Connections are expressed as directed <see cref="Edge" /> records
///     in the containing <see cref="Recipe" />.
/// </summary>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="TextInstructions">Rules for mapping worksheet columns to slide text placeholders.</param>
/// <param name="ImageInstructions">Rules for mapping worksheet columns to slide image shapes.</param>
public record MapNode(
    Point Position,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions)
    : CanvasNode(NodeType.Map, Position);

/// <summary>
///     A free-floating annotation node on the canvas.
/// </summary>
/// <param name="Position">Canvas position of the node.</param>
/// <param name="Color">Background color of the comment card.</param>
/// <param name="Opacity">Opacity of the comment card, in the range [0, 1].</param>
/// <param name="MarkdownText">Markdown-formatted body text.</param>
public record CommentNode(
    Point Position,
    Color Color,
    float Opacity,
    string MarkdownText)
    : CanvasNode(NodeType.Comment, Position);

#endregion

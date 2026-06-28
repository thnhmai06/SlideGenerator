/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: NodeHierarchyTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Domain.Models.Graphs;
using SlideGenerator.Recipe.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Tests for the node hierarchy: <see cref="CanvasNode" /> and <see cref="ChildNode" /> base types,
///     embedded <see cref="WorksheetNode" /> in <see cref="WorkbookNode" />,
///     embedded <see cref="SlideNode" /> in <see cref="PresentationNode" />,
///     and serialization round-trips for both.
/// </summary>
public sealed class NodeHierarchyTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly RecipeRepository _repo;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database. The anchor connection keeps the
    ///     in-memory database alive across all short-lived per-CRUD connections.
    /// </summary>
    public NodeHierarchyTests()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"memory_{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };
        _anchor = new SqliteConnection(builder.ConnectionString);
        _anchor.Open();
        _repo = new RecipeRepository(builder);
    }

    /// <inheritdoc />
    public void Dispose() => _anchor.Dispose();

    #region Type hierarchy

    /// <summary>
    ///     Verifies that <see cref="WorkbookNode" /> is assignable to <see cref="CanvasNode" />.
    /// </summary>
    [Fact]
    public void WorkbookNode_IsCanvasNode()
    {
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier("a.xlsx"), []);
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="PresentationNode" /> is assignable to <see cref="CanvasNode" />.
    /// </summary>
    [Fact]
    public void PresentationNode_IsCanvasNode()
    {
        var node = new PresentationNode("p1", new Point(0, 0), new PresentationIdentifier("t.pptx"), []);
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="MapNode" /> is assignable to <see cref="CanvasNode" />.
    /// </summary>
    [Fact]
    public void MapNode_IsCanvasNode()
    {
        var node = new MapNode("m1", new Point(0, 0), [], []);
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="WorksheetNode" /> is assignable to <see cref="ChildNode" />.
    /// </summary>
    [Fact]
    public void WorksheetNode_IsChildNode()
    {
        var node = new WorksheetNode("ws1", new WorksheetIdentifier("Sheet1"));
        node.Should().BeAssignableTo<ChildNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="SlideNode" /> is assignable to <see cref="ChildNode" />.
    /// </summary>
    [Fact]
    public void SlideNode_IsChildNode()
    {
        var node = new SlideNode("s1", new SlideIdentifier(1));
        node.Should().BeAssignableTo<ChildNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="WorksheetNode" /> (via <see cref="ChildNode" />) is a <see cref="Node" />.
    /// </summary>
    [Fact]
    public void WorksheetNode_IsNode()
    {
        var node = new WorksheetNode("ws1", new WorksheetIdentifier("Sheet1"));
        node.Should().BeAssignableTo<Node>();
    }

    /// <summary>
    ///     Verifies that <see cref="WorkbookNode" /> (via <see cref="CanvasNode" />) is a <see cref="Node" />.
    /// </summary>
    [Fact]
    public void WorkbookNode_IsNode()
    {
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier("a.xlsx"), []);
        node.Should().BeAssignableTo<Node>();
    }

    #endregion

    #region Child containment

    /// <summary>
    ///     Verifies that <see cref="WorkbookNode.Sheets" /> exposes the embedded
    ///     <see cref="WorksheetNode" /> children with the correct ID.
    /// </summary>
    [Fact]
    public void WorkbookNode_WithSheets_SheetsPropertyContainsChildren()
    {
        var sheet = new WorksheetNode("ws1", new WorksheetIdentifier("Sheet1"));
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier("a.xlsx"), [sheet]);

        node.Sheets.Should().HaveCount(1);
        node.Sheets[0].Id.Should().Be("ws1");
    }

    /// <summary>
    ///     Verifies that <see cref="PresentationNode.Slides" /> exposes the embedded
    ///     <see cref="SlideNode" /> children with the correct ID.
    /// </summary>
    [Fact]
    public void PresentationNode_WithSlides_SlidesPropertyContainsChildren()
    {
        var slide = new SlideNode("s1", new SlideIdentifier(1));
        var node = new PresentationNode("p1", new Point(0, 0), new PresentationIdentifier("t.pptx"), [slide]);

        node.Slides.Should().HaveCount(1);
        node.Slides[0].Id.Should().Be("s1");
    }

    #endregion

    #region RecipeGraph type safety

    /// <summary>
    ///     Verifies that <see cref="RecipeGraph.Nodes" /> is typed as
    ///     <see cref="IReadOnlyList{T}" /> of <see cref="CanvasNode" />.
    /// </summary>
    [Fact]
    public void RecipeGraph_Nodes_IsReadOnlyListOfCanvasNode()
    {
        var graph = new RecipeGraph([], []);
        graph.Nodes.Should().BeAssignableTo<IReadOnlyList<CanvasNode>>();
    }

    #endregion

    #region Serialization round-trips

    /// <summary>
    ///     Verifies that a <see cref="WorkbookNode" /> with an embedded <see cref="WorksheetNode" />
    ///     preserves the child's sheet name after a full repository round-trip.
    /// </summary>
    [Fact]
    public async Task WorkbookNode_WithSheet_RoundTripsSheetName()
    {
        var wbPath = Path.GetFullPath("dummy.xlsx");
        var sheet = new WorksheetNode("ws1", new WorksheetIdentifier("SalesData"));
        var node = new WorkbookNode("wb1", new Point(0, 0), new WorkbookIdentifier(wbPath), [sheet]);
        var graph = new RecipeGraph([node], []);

        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var restored = entry.Graph.Nodes.OfType<WorkbookNode>().Single();
        restored.Sheets.Should().HaveCount(1);
        restored.Sheets[0].Worksheet.SheetName.Should().Be("SalesData");
    }

    /// <summary>
    ///     Verifies that a <see cref="PresentationNode" /> with an embedded <see cref="SlideNode" />
    ///     preserves the child's slide index after a full repository round-trip.
    /// </summary>
    [Fact]
    public async Task PresentationNode_WithSlide_RoundTripsSlideIndex()
    {
        var presPath = Path.GetFullPath("template.pptx");
        var slide = new SlideNode("s1", new SlideIdentifier(3));
        var node = new PresentationNode("p1", new Point(0, 0), new PresentationIdentifier(presPath), [slide]);
        var graph = new RecipeGraph([node], []);

        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var restored = entry.Graph.Nodes.OfType<PresentationNode>().Single();
        restored.Slides.Should().HaveCount(1);
        restored.Slides[0].Slide.SlideIndex.Should().Be(3);
    }

    #endregion
}

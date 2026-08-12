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
using SlideGenerator.Document.Models.Sheet;
using SlideGenerator.Document.Models.Slide;
using SlideGenerator.Recipe.Domain.Models;
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
        var node = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier("a.xlsx"), new HashSet<string>());
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="PresentationNode" /> is assignable to <see cref="CanvasNode" />.
    /// </summary>
    [Fact]
    public void PresentationNode_IsCanvasNode()
    {
        var node = new PresentationNode(new Point(0, 0), new PresentationIdentifier("t.pptx"), new HashSet<string>());
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="MapNode" /> is assignable to <see cref="CanvasNode" />.
    /// </summary>
    [Fact]
    public void MapNode_IsCanvasNode()
    {
        var node = new MapNode(new Point(0, 0), [], []);
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="WorksheetNode" /> is assignable to <see cref="ChildNode" />.
    /// </summary>
    [Fact]
    public void WorksheetNode_IsChildNode()
    {
        var node = new WorksheetNode(new WorksheetIdentifier("Sheet1"));
        node.Should().BeAssignableTo<ChildNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="SlideNode" /> is assignable to <see cref="ChildNode" />.
    /// </summary>
    [Fact]
    public void SlideNode_IsChildNode()
    {
        var node = new SlideNode(new SlideIdentifier(1));
        node.Should().BeAssignableTo<ChildNode>();
    }

    /// <summary>
    ///     Verifies that <see cref="WorksheetNode" /> (via <see cref="ChildNode" />) is a <see cref="Node" />.
    /// </summary>
    [Fact]
    public void WorksheetNode_IsNode()
    {
        var node = new WorksheetNode(new WorksheetIdentifier("Sheet1"));
        node.Should().BeAssignableTo<Node>();
    }

    /// <summary>
    ///     Verifies that <see cref="WorkbookNode" /> (via <see cref="CanvasNode" />) is a <see cref="Node" />.
    /// </summary>
    [Fact]
    public void WorkbookNode_IsNode()
    {
        var node = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier("a.xlsx"), new HashSet<string>());
        node.Should().BeAssignableTo<Node>();
    }

    #endregion

    #region Child containment

    /// <summary>
    ///     Verifies that <see cref="WorkbookNode.WorksheetIds" /> exposes the ids of
    ///     child <see cref="WorksheetNode" /> entries.
    /// </summary>
    [Fact]
    public void WorkbookNode_WithSheets_SheetsPropertyContainsChildren()
    {
        var node = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier("a.xlsx"), new HashSet<string> { "ws1" });

        node.WorksheetIds.Should().HaveCount(1);
        node.WorksheetIds.Single().Should().Be("ws1");
    }

    /// <summary>
    ///     Verifies that <see cref="PresentationNode.SlideIds" /> exposes the ids of
    ///     child <see cref="SlideNode" /> entries.
    /// </summary>
    [Fact]
    public void PresentationNode_WithSlides_SlidesPropertyContainsChildren()
    {
        var node = new PresentationNode(new Point(0, 0), new PresentationIdentifier("t.pptx"), new HashSet<string> { "s1" });

        node.SlideIds.Should().HaveCount(1);
        node.SlideIds.Single().Should().Be("s1");
    }

    #endregion

    #region Recipe type safety

    /// <summary>
    ///     Verifies that <see cref="Recipe.Nodes" /> is typed as
    ///     <see cref="IReadOnlyDictionary{TKey,TValue}" /> of <see langword="string" /> → <see cref="Node" />.
    /// </summary>
    [Fact]
    public void RecipeGraph_Nodes_IsReadOnlyDictionaryOfNode()
    {
        var graph = new Domain.Models.Recipe(new Dictionary<string, Node>(), []);
        graph.Nodes.Should().BeAssignableTo<IReadOnlyDictionary<string, Node>>();
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
        var sheet = new WorksheetNode(new WorksheetIdentifier("SalesData"));
        var node = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier(wbPath), new HashSet<string> { "ws1" });
        var graph = new Domain.Models.Recipe(
            new Dictionary<string, Node> { ["wb1"] = node, ["ws1"] = sheet }, []);

        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var restored = entry.Recipe.Nodes.Values.OfType<WorkbookNode>().Single();
        restored.WorksheetIds.Should().HaveCount(1);
        var restoredSheet = (WorksheetNode)entry.Recipe.Nodes[restored.WorksheetIds.Single()];
        restoredSheet.Worksheet.SheetName.Should().Be("SalesData");
    }

    /// <summary>
    ///     Verifies that a <see cref="PresentationNode" /> with an embedded <see cref="SlideNode" />
    ///     preserves the child's slide index after a full repository round-trip.
    /// </summary>
    [Fact]
    public async Task PresentationNode_WithSlide_RoundTripsSlideIndex()
    {
        var presPath = Path.GetFullPath("template.pptx");
        var slide = new SlideNode(new SlideIdentifier(3));
        var node = new PresentationNode(new Point(0, 0), new PresentationIdentifier(presPath), new HashSet<string> { "s1" });
        var graph = new Domain.Models.Recipe(
            new Dictionary<string, Node> { ["p1"] = node, ["s1"] = slide }, []);

        var metadata = await _repo.AddAsync(new RecipeInput("Test", graph), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(metadata.Id, TestContext.Current.CancellationToken);

        var restored = entry.Recipe.Nodes.Values.OfType<PresentationNode>().Single();
        restored.SlideIds.Should().HaveCount(1);
        var restoredSlide = (SlideNode)entry.Recipe.Nodes[restored.SlideIds.Single()];
        restoredSlide.Slide.SlideIndex.Should().Be(3);
    }

    #endregion
}

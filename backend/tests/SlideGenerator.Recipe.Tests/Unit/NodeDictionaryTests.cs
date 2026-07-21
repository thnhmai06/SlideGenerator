/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: NodeDictionaryTests.cs
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
using Newtonsoft.Json;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Recipe.Domain.Models;
using SlideGenerator.Recipe.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Tests for the new <see cref="Recipe.Nodes" /> structure as
///     <see cref="IReadOnlyDictionary{TKey,TValue}" /> keyed by node id,
///     and for the Newtonsoft.Json polymorphic converter on <see cref="Node" />.
/// </summary>
public sealed class NodeDictionaryTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly RecipeRepository _repo;

    /// <summary>Sets up a shared-cache in-memory SQLite database for repository round-trips.</summary>
    public NodeDictionaryTests()
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

    #region Type assertions

    /// <summary>
    ///     Verifies <see cref="Recipe.Nodes" /> is typed as
    ///     <see cref="IReadOnlyDictionary{TKey,TValue}" /> of <see langword="string" /> → <see cref="Node" />.
    /// </summary>
    [Fact]
    public void RecipeGraph_Nodes_IsReadOnlyDictionaryOfNode()
    {
        var graph = new Domain.Models.Recipe(new Dictionary<string, Node>(), []);
        graph.Nodes.Should().BeAssignableTo<IReadOnlyDictionary<string, Node>>();
    }

    /// <summary>
    ///     Verifies <see cref="WorkbookNode" /> no longer requires an <c>id</c> constructor argument.
    /// </summary>
    [Fact]
    public void WorkbookNode_ConstructedWithoutId_IsCanvasNode()
    {
        var node = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier("a.xlsx"), new HashSet<string>());
        node.Should().BeAssignableTo<CanvasNode>();
    }

    /// <summary>
    ///     Verifies <see cref="MapNode" /> no longer requires an <c>id</c> constructor argument.
    /// </summary>
    [Fact]
    public void MapNode_ConstructedWithoutId_IsCanvasNode()
    {
        var node = new MapNode(new Point(0, 0), [], []);
        node.Should().BeAssignableTo<CanvasNode>();
    }

    #endregion

    #region STJ round-trip via repository

    /// <summary>
    ///     Verifies that a <see cref="Recipe" /> with a <see cref="WorkbookNode" /> keyed in the dict
    ///     round-trips through the repository (STJ serialization) with the key preserved.
    /// </summary>
    [Fact]
    public async Task RecipeWithDictNodes_WorkbookNode_PreservesKeyAndSheetName()
    {
        var wbPath = Path.GetFullPath("dummy.xlsx");
        var sheet = new WorksheetNode(new WorksheetIdentifier("SalesData"));
        var wbNode = new WorkbookNode(new Point(10, 20), new WorkbookIdentifier(wbPath), new HashSet<string> { "ws1" });
        var nodes = new Dictionary<string, Node> { ["wb1"] = wbNode, ["ws1"] = sheet };
        var graph = new Domain.Models.Recipe(nodes, []);

        var meta = await _repo.AddAsync(new RecipeInput("DictTest", graph), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(meta.Id, TestContext.Current.CancellationToken);

        entry.Recipe.Nodes.Should().ContainKey("wb1");
        var restored = entry.Recipe.Nodes["wb1"].Should().BeOfType<WorkbookNode>().Subject;
        restored.WorksheetIds.Should().HaveCount(1);
        var restoredSheet = (WorksheetNode)entry.Recipe.Nodes[restored.WorksheetIds.Single()];
        restoredSheet.Worksheet.SheetName.Should().Be("SalesData");
    }

    /// <summary>
    ///     Verifies that multiple canvas node types in a dict all round-trip correctly via STJ.
    /// </summary>
    [Fact]
    public async Task RecipeWithDictNodes_MultipleTypes_AllKeysPreserved()
    {
        var presPath = Path.GetFullPath("template.pptx");
        var nodes = new Dictionary<string, Node>
        {
            ["wb1"] = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier(Path.GetFullPath("a.xlsx")), new HashSet<string>()),
            ["p1"] = new PresentationNode(new Point(200, 0), new PresentationIdentifier(presPath), new HashSet<string>()),
            ["m1"] = new MapNode(new Point(100, 100), [], [])
        };
        var graph = new Domain.Models.Recipe(nodes, []);

        var meta = await _repo.AddAsync(new RecipeInput("MultiType", graph), TestContext.Current.CancellationToken);
        var entry = await _repo.GetAsync(meta.Id, TestContext.Current.CancellationToken);

        entry.Recipe.Nodes.Should().HaveCount(3);
        entry.Recipe.Nodes["wb1"].Should().BeOfType<WorkbookNode>();
        entry.Recipe.Nodes["p1"].Should().BeOfType<PresentationNode>();
        entry.Recipe.Nodes["m1"].Should().BeOfType<MapNode>();
    }

    #endregion

    #region Newtonsoft round-trip (WorkflowCore persistence)

    /// <summary>
    ///     Verifies Newtonsoft.Json serializes and deserializes <see cref="Recipe.Nodes" />
    ///     including all four concrete canvas node types, preserving keys and types.
    /// </summary>
    [Fact]
    public void NodeNewtonsoftConverter_AllConcreteTypes_RoundTrip()
    {
        var presPath = Path.GetFullPath("t.pptx");
        var nodes = new Dictionary<string, Node>
        {
            ["wb1"] = new WorkbookNode(new Point(0, 0), new WorkbookIdentifier(Path.GetFullPath("a.xlsx")), new HashSet<string>()),
            ["p1"] = new PresentationNode(new Point(100, 0), new PresentationIdentifier(presPath), new HashSet<string>()),
            ["m1"] = new MapNode(new Point(50, 50), [], []),
            ["c1"] = new CommentNode(new Point(200, 0), Color.Yellow, 0.8f, "Note")
        };
        var recipe = new Domain.Models.Recipe(nodes, []);

        var json = JsonConvert.SerializeObject(recipe);
        var restored = JsonConvert.DeserializeObject<Domain.Models.Recipe>(json);

        restored.Should().NotBeNull();
        restored.Nodes.Should().HaveCount(4);
        restored.Nodes["wb1"].Should().BeOfType<WorkbookNode>();
        restored.Nodes["p1"].Should().BeOfType<PresentationNode>();
        restored.Nodes["m1"].Should().BeOfType<MapNode>();
        restored.Nodes["c1"].Should().BeOfType<CommentNode>();
        ((CommentNode)restored.Nodes["c1"]).MarkdownText.Should().Be("Note");
    }

    /// <summary>
    ///     Verifies that an empty <see cref="Recipe.Nodes" /> dict round-trips via Newtonsoft.Json.
    /// </summary>
    [Fact]
    public void NodeNewtonsoftConverter_EmptyDict_RoundTrips()
    {
        var recipe = new Domain.Models.Recipe(new Dictionary<string, Node>(), []);
        var json = JsonConvert.SerializeObject(recipe);
        var restored = JsonConvert.DeserializeObject<Domain.Models.Recipe>(json);

        restored.Should().NotBeNull();
        restored.Nodes.Should().BeEmpty();
    }

    #endregion
}

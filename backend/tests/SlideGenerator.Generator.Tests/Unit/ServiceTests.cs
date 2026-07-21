/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: ServiceTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using FluentAssertions;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models.Data;
using SlideGenerator.Generator.Domain.Models.Enum;
using SlideGenerator.Generator.Infrastructure.Services;
using SlideGenerator.Recipe.Domain.Models;
using WorkflowCore.Models;
using Xunit;
using RecipeGraph = SlideGenerator.Recipe.Domain.Models.Recipe;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Service" />'s pure helper methods: <c>BuildJobs</c> (the spawn-phase
///     recipe-graph flattening, ported from the removed <c>LoadRecipeStep</c>) and <c>DeriveStatus</c>
///     (multi-job status aggregation for one request's job list).
/// </summary>
public sealed class ServiceTests
{
    #region BuildJobs

    /// <summary>
    ///     Builds a minimal recipe graph with one WorkbookNode (containing one WorksheetNode),
    ///     one MapNode, and one PresentationNode (containing one SlideNode), connected by edges.
    ///     Edge connectivity: WorksheetNode.Id → MapNode id → SlideNode.Id
    /// </summary>
    private static (RecipeGraph Graph, string WorkbookNodeId, string MapNodeId, string PresentationNodeId,
        string WorksheetNodeId, string SlideNodeId)
        BuildMinimalGraph()
    {
        const string wbId = "wb1";
        const string mapId = "map1";
        const string pptId = "ppt1";
        const string wsId = "ws1";
        const string slideId = "slide1";

        var worksheetNode = new WorksheetNode(new WorksheetIdentifier("Sheet1"));
        var workbookNode = new WorkbookNode(
            Point.Empty,
            new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "data.xlsx")),
            new HashSet<string> { wsId });

        var slideNode = new SlideNode(new SlideIdentifier(1));
        var presentationNode = new PresentationNode(
            Point.Empty,
            new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx")),
            new HashSet<string> { slideId });

        var mapNode = new MapNode(Point.Empty, [], []);

        var nodes = new Dictionary<string, Node>
        {
            [wbId] = workbookNode,
            [pptId] = presentationNode,
            [mapId] = mapNode,
            [wsId] = worksheetNode,
            [slideId] = slideNode
        };
        var edges = new List<Edge>
        {
            new(wsId, mapId),
            new(mapId, slideId)
        };
        var recipe = new RecipeGraph(nodes, edges);

        return (recipe, wbId, mapId, pptId, wsId, slideId);
    }

    /// <summary>
    ///     Verifies that <c>BuildJobs</c> flattens the graph into one <see cref="Job" />
    ///     per (WorksheetNode, MapNode, SlideNode) combination.
    /// </summary>
    [Fact]
    public void BuildJobs_MinimalGraph_ProducesOneJob()
    {
        var (graph, wbId, mapId, pptId, wsId, slideId) = BuildMinimalGraph();
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(graph, request);

        jobs.Should().HaveCount(1);
        var job = jobs[0];
        job.Source.WorkbookNodeId.Should().Be(wbId);
        job.Source.WorksheetNodeId.Should().Be(wsId);
        job.MapNodeId.Should().Be(mapId);
        job.Template.PresentationNodeId.Should().Be(pptId);
        job.Template.SlideNodeId.Should().Be(slideId);
        job.OutputPath.Should().Contain("Sheet1");
    }

    /// <summary>Verifies that a MapNode with no outgoing edge to a SlideNode produces no job.</summary>
    [Fact]
    public void BuildJobs_MapNodeWithNoSlide_ProducesNoJobs()
    {
        const string wsId = "ws1";
        const string mapId = "map1";
        const string wbId = "wb1";

        var worksheetNode = new WorksheetNode(new WorksheetIdentifier("Sheet1"));
        var workbookNode = new WorkbookNode(
            Point.Empty,
            new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "data.xlsx")),
            new HashSet<string> { wsId });
        var mapNode = new MapNode(Point.Empty, [], []);

        var nodes = new Dictionary<string, Node>
        {
            [wbId] = workbookNode,
            [mapId] = mapNode,
            [wsId] = worksheetNode
        };
        var edges = new List<Edge> { new(wsId, mapId) };
        var recipe = new RecipeGraph(nodes, edges);
        var request = new Request(1, "NoSlide", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().BeEmpty();
    }

    #endregion

    #region DeriveStatus

    private static WorkflowInstance Instance(WorkflowStatus status) => new() { Status = status };

    /// <summary>Any job still runnable → the whole request is Running, regardless of other job states.</summary>
    [Fact]
    public void DeriveStatus_AnyRunnable_ReturnsRunning()
    {
        var jobs = new[]
        {
            Instance(WorkflowStatus.Complete),
            Instance(WorkflowStatus.Runnable),
            Instance(WorkflowStatus.Terminated)
        };

        Service.DeriveStatus(jobs).Should().Be(Status.Running);
    }

    /// <summary>No job runnable but at least one suspended → Paused.</summary>
    [Fact]
    public void DeriveStatus_NoneRunnableSomeSuspended_ReturnsPaused()
    {
        var jobs = new[] { Instance(WorkflowStatus.Complete), Instance(WorkflowStatus.Suspended) };

        Service.DeriveStatus(jobs).Should().Be(Status.Paused);
    }

    /// <summary>Every job terminated → Cancelled.</summary>
    [Fact]
    public void DeriveStatus_AllTerminated_ReturnsCancelled()
    {
        var jobs = new[] { Instance(WorkflowStatus.Terminated), Instance(WorkflowStatus.Terminated) };

        Service.DeriveStatus(jobs).Should().Be(Status.Cancelled);
    }

    /// <summary>Every job complete → Complete.</summary>
    [Fact]
    public void DeriveStatus_AllComplete_ReturnsComplete()
    {
        var jobs = new[] { Instance(WorkflowStatus.Complete), Instance(WorkflowStatus.Complete) };

        Service.DeriveStatus(jobs).Should().Be(Status.Complete);
    }

    /// <summary>A mix of Complete and Terminated (no Runnable/Suspended) falls back to Complete.</summary>
    [Fact]
    public void DeriveStatus_MixedCompleteAndTerminated_ReturnsComplete()
    {
        var jobs = new[] { Instance(WorkflowStatus.Complete), Instance(WorkflowStatus.Terminated) };

        Service.DeriveStatus(jobs).Should().Be(Status.Complete);
    }

    #endregion
}

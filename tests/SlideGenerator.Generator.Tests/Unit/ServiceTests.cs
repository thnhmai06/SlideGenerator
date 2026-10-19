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

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Service" />'s pure helper methods: <c>BuildWorkflows</c> (flattens a recipe's
///     <c>Mappings</c> into one <see cref="WorkflowSpecification" /> per worksheet source) and
///     <c>DeriveStatus</c> (multi-workflow status aggregation for one request's workflow list).
/// </summary>
public sealed class ServiceTests
{
    #region BuildWorkflows

    private static Recipe.Models.Recipe OneMappingOneSource()
    {
        var source = new WorksheetSource(
            new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "data.xlsx")),
            new WorksheetIdentifier("Sheet1"));
        var mapping = new Mapping(
            [source],
            new PresentationSource(
                new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx")),
                new SlideIdentifier(1)),
            [], []);
        return new Recipe.Models.Recipe([mapping]);
    }

    /// <summary>Verifies that one mapping with one source produces exactly one workflow with resolved values.</summary>
    [Fact]
    public void BuildWorkflows_OneMappingOneSource_ProducesOneWorkflow()
    {
        var recipe = OneMappingOneSource();
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var workflows = Service.BuildWorkflows(recipe, request);

        workflows.Should().HaveCount(1);
        var workflow = workflows[0];
        workflow.WorksheetName.Should().Be("Sheet1");
        workflow.TemplateSlideIndex.Should().Be(1);
        workflow.OutputPath.Should().Contain("Sheet1");
    }

    /// <summary>Verifies that a mapping with N sources produces N workflows, all sharing the mapping's template/instructions.</summary>
    [Fact]
    public void BuildWorkflows_MappingWithMultipleSources_ProducesOneWorkflowPerSource()
    {
        var mapping = new Mapping(
            [
                new WorksheetSource(new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "a.xlsx")),
                    new WorksheetIdentifier("A")),
                new WorksheetSource(new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "b.xlsx")),
                    new WorksheetIdentifier("B"))
            ],
            new PresentationSource(
                new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx")),
                new SlideIdentifier(1)),
            [], []);
        var recipe = new Recipe.Models.Recipe([mapping]);
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var workflows = Service.BuildWorkflows(recipe, request);

        workflows.Should().HaveCount(2);
        workflows.Select(j => j.WorksheetName).Should().Contain(["A", "B"]);
    }

    /// <summary>Verifies that an empty recipe (no mappings) produces no workflows.</summary>
    [Fact]
    public void BuildWorkflows_EmptyRecipe_ProducesNoWorkflows()
    {
        var recipe = new Recipe.Models.Recipe([]);
        var request = new Request(1, "Empty", PresentationType.Pptx, Path.GetTempPath());

        var workflows = Service.BuildWorkflows(recipe, request);

        workflows.Should().BeEmpty();
    }

    #endregion

    #region DeriveStatus

    private static WorkflowSnapshot Workflow(WorkflowStatus workflowStatus)
    {
        return new WorkflowSnapshot(
            "req", 0, workflowStatus, WorkflowPhase.CreatingOutput, 0,
            new WorkflowSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"),
            DateTimeOffset.UtcNow);
    }

    /// <summary>Any workflow Pending or Running → the whole request is Running.</summary>
    [Fact]
    public void DeriveStatus_AnyPendingOrRunning_ReturnsRunning()
    {
        var workflows = new[] { Workflow(WorkflowStatus.Complete), Workflow(WorkflowStatus.Pending), Workflow(WorkflowStatus.Cancelled) };

        Service.DeriveStatus(workflows).Should().Be(WorkflowStatus.Running);
    }

    /// <summary>No workflow Pending/Running but at least one Paused → Paused.</summary>
    [Fact]
    public void DeriveStatus_NoneRunningSomePaused_ReturnsPaused()
    {
        var workflows = new[] { Workflow(WorkflowStatus.Complete), Workflow(WorkflowStatus.Paused) };

        Service.DeriveStatus(workflows).Should().Be(WorkflowStatus.Paused);
    }

    /// <summary>Every workflow Canceled → Canceled.</summary>
    [Fact]
    public void DeriveStatus_AllCancelled_ReturnsCancelled()
    {
        var workflows = new[] { Workflow(WorkflowStatus.Cancelled), Workflow(WorkflowStatus.Cancelled) };

        Service.DeriveStatus(workflows).Should().Be(WorkflowStatus.Cancelled);
    }

    /// <summary>Every workflow Complete → Complete.</summary>
    [Fact]
    public void DeriveStatus_AllComplete_ReturnsComplete()
    {
        var workflows = new[] { Workflow(WorkflowStatus.Complete), Workflow(WorkflowStatus.Complete) };

        Service.DeriveStatus(workflows).Should().Be(WorkflowStatus.Complete);
    }

    /// <summary>A mix of Complete and Canceled (no Pending/Running/Paused) falls back to Complete.</summary>
    [Fact]
    public void DeriveStatus_MixedCompleteAndCancelled_ReturnsComplete()
    {
        var workflows = new[] { Workflow(WorkflowStatus.Complete), Workflow(WorkflowStatus.Cancelled) };

        Service.DeriveStatus(workflows).Should().Be(WorkflowStatus.Complete);
    }

    #endregion

    #region FindDuplicateOutputPath

    private static WorkflowSpecification Spec(string outputPath, string worksheetName = "Sheet1")
    {
        return new WorkflowSpecification("wb.xlsx", worksheetName, null, null, "template.pptx", 1, [], [], outputPath);
    }

    /// <summary>Two workflows writing to the same output path (e.g. two Mappings sharing a worksheet) are flagged.</summary>
    [Fact]
    public void FindDuplicateOutputPath_TwoWorkflowsSameOutputPath_ReturnsThatPath()
    {
        var workflows = new[] { Spec("out.pptx", "Sheet1"), Spec("out.pptx", "Sheet2") };

        var duplicates = Service.FindDuplicateOutputPath(workflows);

        duplicates.Should().ContainSingle().Which.Should().Be("out.pptx");
    }

    /// <summary>Workflows with all-distinct output paths produce no duplicates.</summary>
    [Fact]
    public void FindDuplicateOutputPath_AllUniqueOutputPaths_ReturnsEmpty()
    {
        var workflows = new[] { Spec("a.pptx"), Spec("b.pptx") };

        Service.FindDuplicateOutputPath(workflows).Should().BeEmpty();
    }

    /// <summary>An empty workflow list has no duplicates.</summary>
    [Fact]
    public void FindDuplicateOutputPath_EmptyWorkflowList_ReturnsEmpty()
    {
        Service.FindDuplicateOutputPath([]).Should().BeEmpty();
    }

    #endregion
}

/// <summary>
///     Unit tests for <see cref="Service" /> instance behavior that needs mocked dependencies:
///     <c>PreviewAsync</c> (per-workflow conflict classification, §8.5) and the <c>includeLogs</c> gate on
///     <c>ListActiveAsync</c>/<c>ListCompletedAsync</c> (§8.4 — list views must never touch the log file).
/// </summary>
public sealed class ServicePreviewAndLoggingTests
{
    private readonly IWorkflowController _workflowController = Substitute.For<IWorkflowController>();
    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly IRequestsRepository _requestsRepository = Substitute.For<IRequestsRepository>();
    private readonly IWorkflowsRepository _workflowsRepository = Substitute.For<IWorkflowsRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ILogFileReader _logFileReader = Substitute.For<ILogFileReader>();
    private readonly ILogger<Service> _logger = NullLogger<Service>.Instance;

    private Service CreateService()
    {
        return new Service(_workflowController, _recipeRepository, _requestsRepository, _workflowsRepository, _eventBus,
            _logFileReader, _logger);
    }

    private static Recipe.Models.Recipe TwoMappingsSameWorksheet()
    {
        var workbook = new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "data.xlsx"));
        var worksheet = new WorksheetIdentifier("Sheet1");
        Mapping Mapping(string templateName) => new(
            [new WorksheetSource(workbook, worksheet)],
            new PresentationSource(
                new PresentationIdentifier(Path.Combine(Path.GetTempPath(), templateName)),
                new SlideIdentifier(1)),
            [], []);
        return new Recipe.Models.Recipe([Mapping("a.pptx"), Mapping("b.pptx")]);
    }

    private void SetupRecipe(Recipe.Models.Recipe recipe)
    {
        _recipeRepository.GetAsync(1, Arg.Any<CancellationToken>())
            .Returns(new RecipeEntry(1, "Test", recipe, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    #region PreviewAsync

    /// <summary>Two mappings sharing a worksheet compute the same output path — both flagged as duplicates.</summary>
    [Fact]
    public async Task PreviewAsync_TwoMappingsSameWorksheet_FlagsDuplicateWithinRequest()
    {
        SetupRecipe(TwoMappingsSameWorksheet());
        _workflowsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkflowSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().HaveCount(2);
        planned.Should().OnlyContain(p => p.ConflictKind == ConflictKind.DuplicateWithinRequest);
    }

    /// <summary>A recipe with no path collisions and no active requests reports every workflow as conflict-free.</summary>
    [Fact]
    public async Task PreviewAsync_NoCollisions_ReturnsNone()
    {
        SetupRecipe(ServiceTests_OneMappingOneSource());
        _workflowsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkflowSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().ContainSingle().Which.ConflictKind.Should().Be(ConflictKind.None);
    }

    /// <summary>A recipe with no mappings plans zero workflows — the "0 workflow" case the Run dialog must block on.</summary>
    [Fact]
    public async Task PreviewAsync_EmptyRecipe_ReturnsEmptyList()
    {
        SetupRecipe(new Recipe.Models.Recipe([]));
        _workflowsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkflowSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().BeEmpty();
    }

    /// <summary>A workflow whose output path is already claimed by another active request is flagged, not thrown.</summary>
    [Fact]
    public async Task PreviewAsync_PathClaimedByActiveRequest_FlagsConflictsWithActiveRequest()
    {
        var recipe = ServiceTests_OneMappingOneSource();
        SetupRecipe(recipe);
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());
        var plannedPath = Service.BuildWorkflows(recipe, request)[0].OutputPath;
        var activeWorkflow = new WorkflowSnapshot("other-request", 0, WorkflowStatus.Running, WorkflowPhase.FillingImages, 5,
            new WorkflowSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], plannedPath), DateTimeOffset.UtcNow);
        _workflowsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkflowSnapshot>>([activeWorkflow]));

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().ContainSingle().Which.ConflictKind.Should().Be(ConflictKind.ConflictsWithActiveRequest);
    }

    private static Recipe.Models.Recipe ServiceTests_OneMappingOneSource()
    {
        var source = new WorksheetSource(
            new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "data.xlsx")),
            new WorksheetIdentifier("Sheet1"));
        var mapping = new Mapping(
            [source],
            new PresentationSource(
                new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx")),
                new SlideIdentifier(1)),
            [], []);
        return new Recipe.Models.Recipe([mapping]);
    }

    #endregion

    #region includeLogs

    /// <summary>
    ///     <c>ListActiveAsync(includeLogs: false)</c> must never read the request's log file — the whole
    ///     point of the flag (§8.4) — and the returned summaries carry no log entries.
    /// </summary>
    [Fact]
    public async Task ListActiveAsync_IncludeLogsFalse_NeverReadsLogFileAndOmitsLogs()
    {
        const string requestId = "req-1";
        var workflow = new WorkflowSnapshot(requestId, 0, WorkflowStatus.Running, WorkflowPhase.FillingText, 3,
            new WorkflowSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"), DateTimeOffset.UtcNow);
        _workflowsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkflowSnapshot>>([workflow]));
        _requestsRepository.GetAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(new RequestRecord(requestId,
                new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()), "unused.log",
                DateTimeOffset.UtcNow));

        var summaries = await CreateService().ListActiveAsync(includeLogs: false);

        _logFileReader.DidNotReceive().ReadAll(Arg.Any<string>());
        summaries.Should().ContainKey(requestId);
        summaries[requestId].Logs.Should().BeEmpty();
        summaries[requestId].Workflows[0].Logs.Should().BeEmpty();
    }

    #endregion
}
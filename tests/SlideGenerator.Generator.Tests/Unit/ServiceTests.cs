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
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Service" />'s pure helper methods: <c>BuildJobs</c> (flattens a recipe's
///     <c>Mappings</c> into one <see cref="JobSpecification" /> per worksheet source) and
///     <c>DeriveStatus</c> (multi-job status aggregation for one request's job list).
/// </summary>
public sealed class ServiceTests
{
    #region BuildJobs

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

    /// <summary>Verifies that one mapping with one source produces exactly one job with resolved values.</summary>
    [Fact]
    public void BuildJobs_OneMappingOneSource_ProducesOneJob()
    {
        var recipe = OneMappingOneSource();
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().HaveCount(1);
        var job = jobs[0];
        job.WorksheetName.Should().Be("Sheet1");
        job.TemplateSlideIndex.Should().Be(1);
        job.OutputPath.Should().Contain("Sheet1");
    }

    /// <summary>Verifies that a mapping with N sources produces N jobs, all sharing the mapping's template/instructions.</summary>
    [Fact]
    public void BuildJobs_MappingWithMultipleSources_ProducesOneJobPerSource()
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

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().HaveCount(2);
        jobs.Select(j => j.WorksheetName).Should().Contain(["A", "B"]);
    }

    /// <summary>Verifies that an empty recipe (no mappings) produces no jobs.</summary>
    [Fact]
    public void BuildJobs_EmptyRecipe_ProducesNoJobs()
    {
        var recipe = new Recipe.Models.Recipe([]);
        var request = new Request(1, "Empty", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().BeEmpty();
    }

    #endregion

    #region DeriveStatus

    private static JobSnapshot Job(JobStatus jobStatus)
    {
        return new JobSnapshot(
            "req", 0, jobStatus, JobPhase.CreatingOutput, 0,
            new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"),
            DateTimeOffset.UtcNow);
    }

    /// <summary>Any job Pending or Running → the whole request is Running.</summary>
    [Fact]
    public void DeriveStatus_AnyPendingOrRunning_ReturnsRunning()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Pending), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Running);
    }

    /// <summary>No job Pending/Running but at least one Paused → Paused.</summary>
    [Fact]
    public void DeriveStatus_NoneRunningSomePaused_ReturnsPaused()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Paused) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Paused);
    }

    /// <summary>Every job Canceled → Canceled.</summary>
    [Fact]
    public void DeriveStatus_AllCancelled_ReturnsCancelled()
    {
        var jobs = new[] { Job(JobStatus.Cancelled), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Cancelled);
    }

    /// <summary>Every job Complete → Complete.</summary>
    [Fact]
    public void DeriveStatus_AllComplete_ReturnsComplete()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Complete) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Complete);
    }

    /// <summary>A mix of Complete and Canceled (no Pending/Running/Paused) falls back to Complete.</summary>
    [Fact]
    public void DeriveStatus_MixedCompleteAndCancelled_ReturnsComplete()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Complete);
    }

    #endregion

    #region FindDuplicateOutputPath

    private static JobSpecification Spec(string outputPath, string worksheetName = "Sheet1")
    {
        return new JobSpecification("wb.xlsx", worksheetName, null, null, "template.pptx", 1, [], [], outputPath);
    }

    /// <summary>Two jobs writing to the same output path (e.g. two Mappings sharing a worksheet) are flagged.</summary>
    [Fact]
    public void FindDuplicateOutputPath_TwoJobsSameOutputPath_ReturnsThatPath()
    {
        var jobs = new[] { Spec("out.pptx", "Sheet1"), Spec("out.pptx", "Sheet2") };

        var duplicates = Service.FindDuplicateOutputPath(jobs);

        duplicates.Should().ContainSingle().Which.Should().Be("out.pptx");
    }

    /// <summary>Jobs with all-distinct output paths produce no duplicates.</summary>
    [Fact]
    public void FindDuplicateOutputPath_AllUniqueOutputPaths_ReturnsEmpty()
    {
        var jobs = new[] { Spec("a.pptx"), Spec("b.pptx") };

        Service.FindDuplicateOutputPath(jobs).Should().BeEmpty();
    }

    /// <summary>An empty job list has no duplicates.</summary>
    [Fact]
    public void FindDuplicateOutputPath_EmptyJobList_ReturnsEmpty()
    {
        Service.FindDuplicateOutputPath([]).Should().BeEmpty();
    }

    #endregion
}

/// <summary>
///     Unit tests for <see cref="Service" /> instance behavior that needs mocked dependencies:
///     <c>PreviewAsync</c> (per-job conflict classification, §8.5) and the <c>includeLogs</c> gate on
///     <c>ListActiveAsync</c>/<c>ListCompletedAsync</c> (§8.4 — list views must never touch the log file).
/// </summary>
public sealed class ServicePreviewAndLoggingTests
{
    private readonly IJobRunner _jobRunner = Substitute.For<IJobRunner>();
    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly IRequestsRepository _requestsRepository = Substitute.For<IRequestsRepository>();
    private readonly IJobsRepository _jobsRepository = Substitute.For<IJobsRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ILogFileReader _logFileReader = Substitute.For<ILogFileReader>();
    private readonly ILogger<Service> _logger = NullLogger<Service>.Instance;

    private Service CreateService()
    {
        return new Service(_jobRunner, _recipeRepository, _requestsRepository, _jobsRepository, _eventBus,
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
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().HaveCount(2);
        planned.Should().OnlyContain(p => p.ConflictKind == ConflictKind.DuplicateWithinRequest);
    }

    /// <summary>A recipe with no path collisions and no active requests reports every job as conflict-free.</summary>
    [Fact]
    public async Task PreviewAsync_NoCollisions_ReturnsNone()
    {
        SetupRecipe(ServiceTests_OneMappingOneSource());
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().ContainSingle().Which.ConflictKind.Should().Be(ConflictKind.None);
    }

    /// <summary>A recipe with no mappings plans zero jobs — the "0 job" case the Run dialog must block on.</summary>
    [Fact]
    public async Task PreviewAsync_EmptyRecipe_ReturnsEmptyList()
    {
        SetupRecipe(new Recipe.Models.Recipe([]));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var planned = await CreateService().PreviewAsync(request);

        planned.Should().BeEmpty();
    }

    /// <summary>A job whose output path is already claimed by another active request is flagged, not thrown.</summary>
    [Fact]
    public async Task PreviewAsync_PathClaimedByActiveRequest_FlagsConflictsWithActiveRequest()
    {
        var recipe = ServiceTests_OneMappingOneSource();
        SetupRecipe(recipe);
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());
        var plannedPath = Service.BuildJobs(recipe, request)[0].OutputPath;
        var activeJob = new JobSnapshot("other-request", 0, JobStatus.Running, JobPhase.FillingImages, 5,
            new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], plannedPath), DateTimeOffset.UtcNow);
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([activeJob]));

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
        var job = new JobSnapshot(requestId, 0, JobStatus.Running, JobPhase.FillingText, 3,
            new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"), DateTimeOffset.UtcNow);
        _jobsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([job]));
        _requestsRepository.GetAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(new RequestRecord(requestId,
                new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()), "unused.log",
                DateTimeOffset.UtcNow));

        var summaries = await CreateService().ListActiveAsync(includeLogs: false);

        _logFileReader.DidNotReceive().ReadAll(Arg.Any<string>());
        summaries.Should().ContainKey(requestId);
        summaries[requestId].Logs.Should().BeEmpty();
        summaries[requestId].Jobs[0].Logs.Should().BeEmpty();
    }

    #endregion
}
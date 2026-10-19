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
using SlideGenerator.Generator.Workflows;
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

    /// <summary>
    ///     Verifies that the request's local-path choice is resolved into every job of the fan-out, so each job
    ///     carries it independently of the originating request.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildJobs_AllowLocalPathsRequest_PropagatesTheFlagToEveryJob(bool allowLocalPaths)
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
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath(), allowLocalPaths);

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().HaveCount(2);
        jobs.Should().OnlyContain(j => j.AllowLocalPaths == allowLocalPaths);
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

        Service.DeriveStatus(jobs).Should().Be(RequestStatus.Running);
    }

    /// <summary>No job Pending/Running but at least one Paused → Paused.</summary>
    [Fact]
    public void DeriveStatus_NoneRunningSomePaused_ReturnsPaused()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Paused) };

        Service.DeriveStatus(jobs).Should().Be(RequestStatus.Paused);
    }

    /// <summary>Every job Canceled → Canceled.</summary>
    [Fact]
    public void DeriveStatus_AllCancelled_ReturnsCancelled()
    {
        var jobs = new[] { Job(JobStatus.Cancelled), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(RequestStatus.Cancelled);
    }

    /// <summary>Every job Complete → Complete.</summary>
    [Fact]
    public void DeriveStatus_AllComplete_ReturnsComplete()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Complete) };

        Service.DeriveStatus(jobs).Should().Be(RequestStatus.Complete);
    }

    /// <summary>A mix of Complete and Canceled (no Pending/Running/Paused) falls back to Complete.</summary>
    [Fact]
    public void DeriveStatus_MixedCompleteAndCancelled_ReturnsComplete()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(RequestStatus.Complete);
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
public sealed class ServicePreviewAndLoggingTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"sg-service-tests-{Guid.NewGuid():N}");
    private readonly IWorkflowController _workflowController = Substitute.For<IWorkflowController>();
    private readonly IRecipeRepository _recipeRepository = Substitute.For<IRecipeRepository>();
    private readonly IWorkflowsRepository _workflowsRepository = Substitute.For<IWorkflowsRepository>();
    private readonly IJobsRepository _jobsRepository = Substitute.For<IJobsRepository>();
    private readonly ILogFileReader _logFileReader = Substitute.For<ILogFileReader>();
    private readonly ILogger<Service> _logger = NullLogger<Service>.Instance;

    /// <summary>
    ///     Creates a fresh temporary directory holding the real (empty) workbook and presentation files the planner
    ///     requires to report no <see cref="JobProblem" />.
    /// </summary>
    public ServicePreviewAndLoggingTests()
    {
        Directory.CreateDirectory(_tempDir);
        foreach (var name in new[] { "data.xlsx", "a.xlsx", "b.xlsx", "a.pptx", "b.pptx", "template.pptx" })
            File.WriteAllBytes(Path.Combine(_tempDir, name), []);
    }

    /// <summary>Deletes the temporary directory created for this test instance.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private string TempPath(string fileName)
    {
        return Path.Combine(_tempDir, fileName);
    }

    private Service CreateService()
    {
        return new Service(_workflowController, _recipeRepository, _workflowsRepository, _jobsRepository,
            _logFileReader, _logger);
    }

    private Recipe.Models.Recipe TwoMappingsSameWorksheet()
    {
        var workbook = new WorkbookIdentifier(TempPath("data.xlsx"));
        var worksheet = new WorksheetIdentifier("Sheet1");
        Mapping Mapping(string templateName) => new(
            [new WorksheetSource(workbook, worksheet)],
            new PresentationSource(
                new PresentationIdentifier(TempPath(templateName)),
                new SlideIdentifier(1)),
            [], []);
        return new Recipe.Models.Recipe([Mapping("a.pptx"), Mapping("b.pptx")]);
    }

    private Recipe.Models.Recipe OneMapping(string workbookPath, string templatePath, int slideIndex,
        RowFilter? rowFilter = null)
    {
        var source = new WorksheetSource(
            new WorkbookIdentifier(workbookPath),
            new WorksheetIdentifier("Sheet1"),
            null,
            rowFilter);
        var mapping = new Mapping(
            [source],
            new PresentationSource(
                new PresentationIdentifier(templatePath),
                new SlideIdentifier(1) { SlideIndex = slideIndex }),
            [], []);
        return new Recipe.Models.Recipe([mapping]);
    }

    private Recipe.Models.Recipe ServiceTests_OneMappingOneSource()
    {
        return OneMapping(TempPath("data.xlsx"), TempPath("template.pptx"), 1);
    }

    private void SetupRecipe(Recipe.Models.Recipe recipe)
    {
        _recipeRepository.GetAsync(1, Arg.Any<CancellationToken>())
            .Returns(new RecipeEntry(1, "Test", recipe, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    /// <summary>Row filters that select nothing by construction, regardless of the worksheet's row count.</summary>
    public static TheoryData<RowFilter> InvalidRowFilters => new()
    {
        new IndexRangeFilter(0, 5),
        new IndexRangeFilter(5, 2),
        new PartitionBlockFilter(0, 0),
        new PartitionBlockFilter(3, 3),
        new PartitionBlockFilter(-1, 2)
    };

    /// <summary>Row filters that are valid by construction (including no filter at all).</summary>
    public static TheoryData<RowFilter?> ValidRowFilters => new()
    {
        (RowFilter?)null,
        new AllRowFilter(),
        new IndexRangeFilter(1, 5),
        new PartitionBlockFilter(1, 3)
    };

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

    /// <summary>A job whose source workbook is missing is reported as <see cref="JobProblem.WorkbookNotFound" />.</summary>
    [Fact]
    public async Task PreviewAsync_MissingWorkbook_ReportsWorkbookNotFound()
    {
        SetupRecipe(OneMapping(Path.Combine(_tempDir, "missing.xlsx"), TempPath("template.pptx"), 1));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var planned = await CreateService().PreviewAsync(
            new Request(1, "Test", PresentationType.Pptx, _tempDir), TestContext.Current.CancellationToken);

        var job = planned.Should().ContainSingle().Subject;
        job.Problem.Should().Be(JobProblem.WorkbookNotFound);
        job.IsBlocked.Should().BeTrue();
    }

    /// <summary>A job whose template presentation is missing is reported as <see cref="JobProblem.TemplateNotFound" />.</summary>
    [Fact]
    public async Task PreviewAsync_MissingTemplate_ReportsTemplateNotFound()
    {
        SetupRecipe(OneMapping(TempPath("data.xlsx"), Path.Combine(_tempDir, "missing.pptx"), 1));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var planned = await CreateService().PreviewAsync(
            new Request(1, "Test", PresentationType.Pptx, _tempDir), TestContext.Current.CancellationToken);

        var job = planned.Should().ContainSingle().Subject;
        job.Problem.Should().Be(JobProblem.TemplateNotFound);
        job.IsBlocked.Should().BeTrue();
    }

    /// <summary>A template slide index below 1 is reported as <see cref="JobProblem.InvalidTemplateSlide" />.</summary>
    [Fact]
    public async Task PreviewAsync_TemplateSlideIndexZero_ReportsInvalidTemplateSlide()
    {
        SetupRecipe(OneMapping(TempPath("data.xlsx"), TempPath("template.pptx"), 0));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var planned = await CreateService().PreviewAsync(
            new Request(1, "Test", PresentationType.Pptx, _tempDir), TestContext.Current.CancellationToken);

        var job = planned.Should().ContainSingle().Subject;
        job.Problem.Should().Be(JobProblem.InvalidTemplateSlide);
        job.IsBlocked.Should().BeTrue();
    }

    /// <summary>
    ///     Row filters that select nothing by construction are reported as
    ///     <see cref="JobProblem.InvalidRowFilter" /> before the job ever starts.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidRowFilters))]
    public async Task PreviewAsync_InvalidRowFilter_ReportsInvalidRowFilter(RowFilter rowFilter)
    {
        SetupRecipe(OneMapping(TempPath("data.xlsx"), TempPath("template.pptx"), 1, rowFilter));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var planned = await CreateService().PreviewAsync(
            new Request(1, "Test", PresentationType.Pptx, _tempDir), TestContext.Current.CancellationToken);

        var job = planned.Should().ContainSingle().Subject;
        job.Problem.Should().Be(JobProblem.InvalidRowFilter);
        job.IsBlocked.Should().BeTrue();
    }

    /// <summary>Valid row filters (including no filter at all) report no problem.</summary>
    [Theory]
    [MemberData(nameof(ValidRowFilters))]
    public async Task PreviewAsync_ValidRowFilter_ReportsNoProblem(RowFilter? rowFilter)
    {
        SetupRecipe(OneMapping(TempPath("data.xlsx"), TempPath("template.pptx"), 1, rowFilter));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var planned = await CreateService().PreviewAsync(
            new Request(1, "Test", PresentationType.Pptx, _tempDir), TestContext.Current.CancellationToken);

        planned.Should().ContainSingle().Which.Problem.Should().Be(JobProblem.None);
    }

    /// <summary>
    ///     An already existing output file is reported as an overwrite warning — it does not block the request, and
    ///     <c>CreateAsync</c> still starts the workflow.
    /// </summary>
    [Fact]
    public async Task PreviewAsync_OutputFileAlreadyExists_ReportsOverwriteWarningThatDoesNotBlock()
    {
        var recipe = ServiceTests_OneMappingOneSource();
        SetupRecipe(recipe);
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));
        var request = new Request(1, "Test", PresentationType.Pptx, _tempDir);
        var outputPath = Service.BuildJobs(recipe, request)[0].OutputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, []);

        var planned = await CreateService().PreviewAsync(request, TestContext.Current.CancellationToken);

        var job = planned.Should().ContainSingle().Subject;
        job.ConflictKind.Should().Be(ConflictKind.OverwritesExistingFile);
        job.IsBlocked.Should().BeFalse();

        var workflowId = await CreateService().CreateAsync(request, TestContext.Current.CancellationToken);

        workflowId.Should().NotBeNullOrEmpty();
        await _workflowController.Received(1).StartWorkflowAsync(workflowId,
            Arg.Any<IReadOnlyList<JobSpecification>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     <c>CreateAsync</c> refuses a request whose first job has an input problem, names that problem, and starts
    ///     and persists nothing.
    /// </summary>
    [Fact]
    public async Task CreateAsync_JobWithMissingWorkbook_ThrowsAndStartsNothing()
    {
        SetupRecipe(OneMapping(Path.Combine(_tempDir, "missing.xlsx"), TempPath("template.pptx"), 1));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var act = () => CreateService().CreateAsync(
            new Request(1, "Test", PresentationType.Pptx, _tempDir), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Job 0 (Sheet1 of missing.xlsx) cannot run: WorkbookNotFound*");
        await _workflowController.DidNotReceive().StartWorkflowAsync(Arg.Any<string>(),
            Arg.Any<IReadOnlyList<JobSpecification>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _workflowsRepository.DidNotReceive().CreateAsync(Arg.Any<WorkflowRecord>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     <see cref="PlannedJob.IsBlocked" /> is true for input problems and real conflicts, but not for the
    ///     non-blocking overwrite warning.
    /// </summary>
    [Fact]
    public void PlannedJob_IsBlocked_OnlyForProblemsAndRealConflicts()
    {
        static PlannedJob Planned(ConflictKind conflictKind, JobProblem problem = JobProblem.None)
        {
            return new PlannedJob("out.pptx", "wb.xlsx", "Sheet1", conflictKind, problem);
        }

        Planned(ConflictKind.None).IsBlocked.Should().BeFalse();
        Planned(ConflictKind.OverwritesExistingFile).IsBlocked.Should().BeFalse();
        Planned(ConflictKind.DuplicateWithinRequest).IsBlocked.Should().BeTrue();
        Planned(ConflictKind.ConflictsWithActiveRequest).IsBlocked.Should().BeTrue();

        Planned(ConflictKind.None).HasBlockingConflict.Should().BeFalse();
        Planned(ConflictKind.OverwritesExistingFile).HasBlockingConflict.Should().BeFalse();
        Planned(ConflictKind.DuplicateWithinRequest).HasBlockingConflict.Should().BeTrue();
        Planned(ConflictKind.ConflictsWithActiveRequest).HasBlockingConflict.Should().BeTrue();

        foreach (var problem in Enum.GetValues<JobProblem>().Where(p => p != JobProblem.None))
            Planned(ConflictKind.None, problem).IsBlocked.Should().BeTrue();
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
        const string workflowId = "req-1";
        var job = new JobSnapshot(workflowId, 0, JobStatus.Running, JobPhase.FillingText, 3,
            new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"), DateTimeOffset.UtcNow);
        _jobsRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([job]));
        _workflowsRepository.GetAsync(workflowId, Arg.Any<CancellationToken>())
            .Returns(new WorkflowRecord(workflowId,
                new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()), "unused.log",
                DateTimeOffset.UtcNow));
        _workflowsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkflowRecord>>([]));

        var summaries = await CreateService().ListActiveAsync(includeLogs: false);

        _logFileReader.DidNotReceive().ReadAll(Arg.Any<string>());
        summaries.Should().ContainKey(workflowId);
        summaries[workflowId].Logs.Should().BeEmpty();
        summaries[workflowId].Jobs[0].Logs.Should().BeEmpty();
    }

    /// <summary>
    ///     A request whose jobs all errored must still be listed as completed, with <see cref="RequestStatus.Error" />;
    ///     a request mixing Complete and Error reports <see cref="RequestStatus.CompletedWithError" />.
    /// </summary>
    [Fact]
    public async Task ListCompletedAsync_AllJobsErrored_IsListedAsCompletedWithErrorStatus()
    {
        const string errored = "wf-errored";
        const string mixed = "wf-mixed";
        var spec = new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx");
        IReadOnlyList<JobSnapshot> jobs =
        [
            new JobSnapshot(errored, 0, JobStatus.Error, JobPhase.FillingText, 0, spec, DateTimeOffset.UtcNow),
            new JobSnapshot(errored, 1, JobStatus.Error, JobPhase.FillingText, 0, spec, DateTimeOffset.UtcNow),
            new JobSnapshot(mixed, 0, JobStatus.Complete, JobPhase.Done, 1, spec, DateTimeOffset.UtcNow),
            new JobSnapshot(mixed, 1, JobStatus.Error, JobPhase.FillingText, 0, spec, DateTimeOffset.UtcNow)
        ];
        _jobsRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(jobs));
        WorkflowRecord Record(string workflowId) => new(workflowId,
            new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()), "unused.log",
            DateTimeOffset.UtcNow, JobStatus.Complete);
        _workflowsRepository.GetAsync(errored, Arg.Any<CancellationToken>()).Returns(Record(errored));
        _workflowsRepository.GetAsync(mixed, Arg.Any<CancellationToken>()).Returns(Record(mixed));

        var summaries = await CreateService().ListCompletedAsync(includeLogs: false, TestContext.Current.CancellationToken);

        summaries.Should().ContainKey(errored);
        summaries[errored].Status.Should().Be(RequestStatus.Error);
        summaries.Should().ContainKey(mixed);
        summaries[mixed].Status.Should().Be(RequestStatus.CompletedWithError);
    }

    #endregion

    #region Workflow control

    private void SetupWorkflow(string workflowId, JobStatus workflowStatus, params JobStatus[] jobStatuses)
    {
        var spec = new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx");
        IReadOnlyList<JobSnapshot> jobs =
        [
            .. jobStatuses.Select((status, i) =>
                new JobSnapshot(workflowId, i, status, JobPhase.FillingText, 0, spec, DateTimeOffset.UtcNow))
        ];
        _jobsRepository.GetByWorkflowIdAsync(workflowId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(jobs));
        _workflowsRepository.GetAsync(workflowId, Arg.Any<CancellationToken>())
            .Returns(new WorkflowRecord(workflowId, new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()),
                "unused.log", DateTimeOffset.UtcNow, workflowStatus));
    }

    /// <summary>A running workflow is paused with exactly one workflow-level call, never one call per job.</summary>
    [Fact]
    public async Task PauseAsync_RunningWorkflow_PausesTheWorkflowOnce()
    {
        SetupWorkflow("wf", JobStatus.Running, JobStatus.Running, JobStatus.Running, JobStatus.Pending);
        _workflowController.PauseWorkflowAsync("wf").Returns(true);

        var result = await CreateService().PauseAsync("wf");

        result.Should().Be(new PartialResult(1, 0));
        await _workflowController.Received(1).PauseWorkflowAsync("wf");
    }

    /// <summary>Stopping a workflow whose jobs have all finished is skipped, not failed, and calls nothing.</summary>
    [Fact]
    public async Task StopAsync_FinishedWorkflow_IsSkipped()
    {
        SetupWorkflow("wf", JobStatus.Complete, JobStatus.Complete, JobStatus.Complete);

        var result = await CreateService().StopAsync("wf");

        result.Should().Be(new PartialResult(0, 1));
        await _workflowController.DidNotReceive().StopWorkflowAsync(Arg.Any<string>());
    }

    /// <summary>A workflow paused at workflow level is resumable even though its job rows still read as running.</summary>
    [Fact]
    public async Task ResumeAsync_PausedWorkflowWithRunningJobRows_ResumesTheWorkflow()
    {
        SetupWorkflow("wf", JobStatus.Paused, JobStatus.Running, JobStatus.Pending);
        _workflowController.ResumeWorkflowAsync("wf").Returns(true);

        var result = await CreateService().ResumeAsync("wf");

        result.Should().Be(new PartialResult(1, 0));
    }

    /// <summary>
    ///     <c>CreateAsync</c> registers one workflow holding every computed job, in a single controller call, and
    ///     persists the workflow row first.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TwoSources_StartsOneWorkflowWithTwoJobs()
    {
        var mapping = new Mapping(
            [
                new WorksheetSource(new WorkbookIdentifier(TempPath("a.xlsx")),
                    new WorksheetIdentifier("A")),
                new WorksheetSource(new WorkbookIdentifier(TempPath("b.xlsx")),
                    new WorksheetIdentifier("B"))
            ],
            new PresentationSource(
                new PresentationIdentifier(TempPath("template.pptx")),
                new SlideIdentifier(1)),
            [], []);
        SetupRecipe(new Recipe.Models.Recipe([mapping]));
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));

        var workflowId = await CreateService()
            .CreateAsync(new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()));

        await _workflowsRepository.Received(1).CreateAsync(
            Arg.Is<WorkflowRecord>(r => r.WorkflowId == workflowId && r.Status == JobStatus.Pending),
            Arg.Any<CancellationToken>());
        await _workflowController.Received(1).StartWorkflowAsync(workflowId,
            Arg.Is<IReadOnlyList<JobSpecification>>(jobs => jobs.Count == 2), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Two concurrent requests claiming the same output path must not both pass the conflict check before either
    ///     has persisted its job rows — the second is rejected once the first's rows become visible.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TwoConcurrentRequestsSameOutputPath_OnlyOneIsStarted()
    {
        SetupRecipe(ServiceTests_OneMappingOneSource());
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var persisted = new List<JobSnapshot>();
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<JobSnapshot>>([.. persisted]));

        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _workflowController.StartWorkflowAsync(Arg.Any<string>(),
                Arg.Any<IReadOnlyList<JobSpecification>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                firstStarted.TrySetResult();
                await release.Task;
                var workflowId = ci.ArgAt<string>(0);
                var specs = ci.ArgAt<IReadOnlyList<JobSpecification>>(1);
                persisted.AddRange(specs.Select((spec, index) =>
                    new JobSnapshot(workflowId, index, JobStatus.Pending, JobPhase.CreatingOutput, 0, spec,
                        DateTimeOffset.UtcNow)));
            });

        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        var first = service.CreateAsync(request, ct);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var second = service.CreateAsync(request, ct);

        release.SetResult();
        var firstId = await first;

        Func<Task> secondCall = () => second;
        await secondCall.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already being generated*");
        await _workflowController.Received(1).StartWorkflowAsync(Arg.Any<string>(),
            Arg.Any<IReadOnlyList<JobSpecification>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        firstId.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    ///     A workflow stored as paused whose job rows still derive as running reports as paused; once every job has
    ///     finished the stored pause no longer matters.
    /// </summary>
    [Fact]
    public void DeriveStatus_WorkflowPaused_OnlyOverridesRunningJobs()
    {
        var spec = new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx");
        JobSnapshot Job(JobStatus status) =>
            new("wf", 0, status, JobPhase.FillingText, 0, spec, DateTimeOffset.UtcNow);

        Service.DeriveStatus([Job(JobStatus.Running)], JobStatus.Paused).Should().Be(RequestStatus.Paused);
        Service.DeriveStatus([Job(JobStatus.Complete)], JobStatus.Paused).Should().Be(RequestStatus.Complete);
        Service.DeriveStatus([Job(JobStatus.Running)], JobStatus.Running).Should().Be(RequestStatus.Running);
    }

    #endregion

    #region Log path and deletion

    private static string CreateTempLogFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");
        File.WriteAllText(path, "log");
        return path;
    }

    /// <summary>
    ///     Two requests sharing the same user-chosen name must still get distinct log files, each named after its
    ///     own workflow id — otherwise both workflows would append into one mixed file.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TwoRequestsWithSameName_PersistDistinctLogPathsNamedAfterWorkflowId()
    {
        SetupRecipe(ServiceTests_OneMappingOneSource());
        _jobsRepository.GetNonTerminalAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<JobSnapshot>>([]));
        List<WorkflowRecord> records = [];
        await _workflowsRepository.CreateAsync(Arg.Do<WorkflowRecord>(records.Add), Arg.Any<CancellationToken>());
        var ct = TestContext.Current.CancellationToken;
        var service = CreateService();
        var first = new Request(1, "Same Name", PresentationType.Pptx,
            Path.Combine(Path.GetTempPath(), $"out-a-{Guid.NewGuid():N}"));
        var second = new Request(1, "Same Name", PresentationType.Pptx,
            Path.Combine(Path.GetTempPath(), $"out-b-{Guid.NewGuid():N}"));

        var firstId = await service.CreateAsync(first, ct);
        var secondId = await service.CreateAsync(second, ct);

        records.Should().HaveCount(2);
        records[0].LogPath.Should().NotBe(records[1].LogPath);
        Path.GetFileNameWithoutExtension(records[0].LogPath).Should().Be(firstId);
        Path.GetFileNameWithoutExtension(records[1].LogPath).Should().Be(secondId);
    }

    /// <summary>Deleting a workflow removes its log file along with its rows.</summary>
    [Fact]
    public async Task DeleteAsync_WorkflowWithLogFile_DeletesTheLogFile()
    {
        const string workflowId = "wf-log";
        var logPath = CreateTempLogFile();
        try
        {
            SetupWorkflow(workflowId, JobStatus.Complete, JobStatus.Complete);
            _workflowsRepository.GetAsync(workflowId, Arg.Any<CancellationToken>())
                .Returns(new WorkflowRecord(workflowId,
                    new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()), logPath,
                    DateTimeOffset.UtcNow, JobStatus.Complete));

            var deleted = await CreateService().DeleteAsync(workflowId, TestContext.Current.CancellationToken);

            deleted.Should().BeTrue();
            File.Exists(logPath).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
        }
    }

    /// <summary>Deleting a workflow whose log file is already gone still deletes its rows and reports success.</summary>
    [Fact]
    public async Task DeleteAsync_LogFileAlreadyMissing_StillDeletesRows()
    {
        const string workflowId = "wf-no-log";
        var logPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");
        SetupWorkflow(workflowId, JobStatus.Complete, JobStatus.Complete);
        _workflowsRepository.GetAsync(workflowId, Arg.Any<CancellationToken>())
            .Returns(new WorkflowRecord(workflowId,
                new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath()), logPath,
                DateTimeOffset.UtcNow, JobStatus.Complete));

        var deleted = await CreateService().DeleteAsync(workflowId, TestContext.Current.CancellationToken);

        deleted.Should().BeTrue();
        await _workflowsRepository.Received(1).DeleteAsync(workflowId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Deleting an active request stops its workflow with <c>waitForExit</c> before removing rows, so the workflow
    ///     cannot re-write its job rows or log file after they are deleted.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ActiveWorkflow_StopsAndWaitsForExitBeforeDeletingRows()
    {
        const string workflowId = "wf-active";
        SetupWorkflow(workflowId, JobStatus.Running, JobStatus.Running);
        _workflowController.StopWorkflowAsync(workflowId, true).Returns(true);

        var deleted = await CreateService().DeleteAsync(workflowId, TestContext.Current.CancellationToken);

        deleted.Should().BeTrue();
        Received.InOrder(() =>
        {
            _workflowController.StopWorkflowAsync(workflowId, true);
            _jobsRepository.DeleteByWorkflowIdAsync(workflowId, Arg.Any<CancellationToken>());
        });
    }

    /// <summary>Bulk-deleting completed workflows removes every one of their log files.</summary>
    [Fact]
    public async Task DeleteAllCompletedAsync_CompletedWorkflows_DeletesTheirLogFiles()
    {
        const string firstId = "wf-done-1";
        const string secondId = "wf-done-2";
        var firstLog = CreateTempLogFile();
        var secondLog = CreateTempLogFile();
        try
        {
            var spec = new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx");
            IReadOnlyList<JobSnapshot> jobs =
            [
                new JobSnapshot(firstId, 0, JobStatus.Complete, JobPhase.Done, 1, spec, DateTimeOffset.UtcNow),
                new JobSnapshot(secondId, 0, JobStatus.Complete, JobPhase.Done, 1, spec, DateTimeOffset.UtcNow)
            ];
            _jobsRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(jobs));
            _workflowsRepository.GetAsync(firstId, Arg.Any<CancellationToken>())
                .Returns(new WorkflowRecord(firstId,
                    new Request(1, "A", PresentationType.Pptx, Path.GetTempPath()), firstLog,
                    DateTimeOffset.UtcNow, JobStatus.Complete));
            _workflowsRepository.GetAsync(secondId, Arg.Any<CancellationToken>())
                .Returns(new WorkflowRecord(secondId,
                    new Request(1, "B", PresentationType.Pptx, Path.GetTempPath()), secondLog,
                    DateTimeOffset.UtcNow, JobStatus.Complete));

            var deleted = await CreateService().DeleteAllCompletedAsync(TestContext.Current.CancellationToken);

            deleted.Should().Be(2);
            File.Exists(firstLog).Should().BeFalse();
            File.Exists(secondLog).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(firstLog)) File.Delete(firstLog);
            if (File.Exists(secondLog)) File.Delete(secondLog);
        }
    }

    #endregion
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Service.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Utilities;

namespace SlideGenerator.Generator;

/// <summary>
///     Defines the facade for job lifecycle operations exposed to the IPC layer.
///     Consumers (e.g. <c>GeneratingActiveHandler</c>) depend only on this interface and are
///     completely decoupled from <see cref="IWorkflowController" /> internals.
/// </summary>
public interface IService
{
    /// <summary>
    ///     Resumes any crashed non-terminal job and starts the underlying job runner.
    ///     Must be called once during application startup before any other method.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    ///     Stops the underlying job runner gracefully.
    ///     Must be called during application shutdown.
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Computes the job list <see cref="CreateAsync" /> would spawn for <paramref name="request" />,
    ///     without spawning anything — lets a caller (e.g. a run confirmation dialog) show exactly what will
    ///     be created, flag every output-path conflict and input problem (<see cref="JobProblem" />)
    ///     <see cref="CreateAsync" /> would otherwise reject at submit time, using the identical definition,
    ///     and warn about jobs that would overwrite an existing file
    ///     (<see cref="ConflictKind.OverwritesExistingFile" />, which does not block). Does not itself throw on
    ///     conflicts or problems — they are reported per job via <see cref="PlannedJob.ConflictKind" /> and
    ///     <see cref="PlannedJob.Problem" /> instead.
    /// </summary>
    Task<IReadOnlyList<PlannedJob>> PreviewAsync(Request request, CancellationToken ct = default);

    /// <summary>
    ///     Starts a new slide-generation request for the given <paramref name="request" /> — spawns one
    ///     job per computed job specification. Multiple active requests for the same recipe are
    ///     allowed; throws if a job has an input problem (its source workbook or template presentation is
    ///     missing, its template slide index is below 1, or its row filter selects nothing), if two of the
    ///     request's own jobs would write to the same output path, or if a computed job's output path collides
    ///     with one already claimed by another active (running or paused) request's job.
    /// </summary>
    /// <returns>The generated workflow id grouping every job spawned for it.</returns>
    Task<string> CreateAsync(Request request, CancellationToken ct = default);

    /// <summary>
    ///     Cancels every job belonging to <paramref name="workflowId" /> that is still runnable or paused.
    ///     Best-effort — jobs already complete/terminated are counted as skipped.
    /// </summary>
    Task<PartialResult> StopAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    ///     Pauses every job belonging to <paramref name="workflowId" /> that is still running.
    ///     Best-effort — jobs not currently running are counted as skipped.
    /// </summary>
    Task<PartialResult> PauseAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    ///     Resumes every job belonging to <paramref name="workflowId" /> that is currently paused.
    ///     Best-effort — jobs not currently paused are counted as skipped.
    /// </summary>
    Task<PartialResult> ResumeAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    ///     Stops every currently active (running or paused) request. Best-effort.
    /// </summary>
    /// <returns>The number of requests successfully stopped.</returns>
    Task<int> StopAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Pauses every currently running (non-paused) request. Best-effort.
    /// </summary>
    /// <returns>The number of requests successfully paused.</returns>
    Task<int> PauseAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Returns summaries of all currently active (running or paused) requests, keyed by workflow id.
    /// </summary>
    /// <param name="includeLogs">
    ///     When <see langword="false" />, <see cref="Summary.Logs" />/<see cref="JobSummary.Logs" /> are
    ///     empty and no request's log file is read from disk — pass <see langword="false" /> when only the
    ///     list itself is needed (e.g. rendering a list of requests), since every group would otherwise pay
    ///     for a full read-and-parse of its <c>.log</c> file even though the list view never shows it.
    /// </param>
    /// <param name="ct"></param>
    Task<IReadOnlyDictionary<string, Summary>> ListActiveAsync(bool includeLogs = true, CancellationToken ct = default);

    /// <summary>
    ///     Returns summaries of all completed, canceled, or errored requests, keyed by workflow id.
    /// </summary>
    /// <param name="includeLogs">See <see cref="ListActiveAsync" />.</param>
    /// <param name="ct"></param>
    Task<IReadOnlyDictionary<string, Summary>> ListCompletedAsync(bool includeLogs = true, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes every job belonging to a request. If the request is still
    ///     active (running or paused), it is stopped first.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the request was found and deleted;
    ///     <see langword="false" /> if not found.
    /// </returns>
    Task<bool> DeleteAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes all completed and canceled requests and their associated data.
    /// </summary>
    /// <returns>The number of requests deleted.</returns>
    Task<int> DeleteAllCompletedAsync(CancellationToken ct = default);
}

/// <summary>
///     Implements <see cref="IService" /> on top of <see cref="IWorkflowController" />. A request runs as one
///     workflow whose jobs (one per worksheet/presentation pair) are created here; <c>CreateAsync</c> only
///     registers the workflow and returns, deliberately not itself throttled by <c>MaxConcurrentJobs</c> — see
///     <see cref="IWorkflowController" /> remarks.
/// </summary>
internal sealed class Service(
    IWorkflowController workflowController,
    IRecipeRepository recipeRepository,
    IWorkflowsRepository workflowsRepository,
    IJobsRepository jobsRepository,
    ILogFileReader logFileReader,
    ILogger<Service> logger)
    : IService
{
    private readonly SemaphoreSlim _createLock = new(1, 1);

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken ct = default)
    {
        return workflowController.InitializeAsync(ct);
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return workflowController.ShutdownAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlannedJob>> PreviewAsync(Request request, CancellationToken ct = default)
    {
        var entry = await recipeRepository.GetAsync(request.RecipeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Recipe {request.RecipeId} not found.");

        var jobs = BuildJobs(entry.Recipe, request);
        return await PlanAsync(jobs).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> CreateAsync(Request request, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope("Start/{RecipeId}/{RequestName}", request.RecipeId, request.Name);

        var entry = await recipeRepository.GetAsync(request.RecipeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Recipe {request.RecipeId} not found.");

        var workflowId = Guid.NewGuid().ToString();
        var logPath = ResolveWorkflowLogPath(workflowId);
        var jobs = BuildJobs(entry.Recipe, request);

        await _createLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var planned = await PlanAsync(jobs).ConfigureAwait(false);
            var blockedIndex = -1;
            for (var i = 0; i < planned.Count; i++)
                if (planned[i].Problem != JobProblem.None)
                {
                    blockedIndex = i;
                    break;
                }

            if (blockedIndex >= 0)
            {
                var blockedJob = jobs[blockedIndex];
                throw new InvalidOperationException(
                    $"Job {blockedIndex} ({blockedJob.WorksheetName} of {Path.GetFileName(blockedJob.WorkbookPath)}) " +
                    $"cannot run: {planned[blockedIndex].Problem}.");
            }

            var duplicatePaths = FindDuplicateOutputPath(jobs);
            if (duplicatePaths.Count > 0)
                throw new InvalidOperationException(
                    $"Output path '{duplicatePaths.First()}' is used by more than one mapping in this recipe.");

            var conflictingPath = await FindConflictingOutputPathAsync(jobs).ConfigureAwait(false);
            if (conflictingPath is not null)
                throw new InvalidOperationException(
                    $"Output path '{conflictingPath}' is already being generated by an active request.");

            await workflowsRepository.CreateAsync(
                new WorkflowRecord(workflowId, request, logPath, DateTimeOffset.UtcNow), ct).ConfigureAwait(false);

            await workflowController.StartWorkflowAsync(workflowId, jobs, logPath, ct).ConfigureAwait(false);
        }
        finally
        {
            _createLock.Release();
        }

        logger.LogInformation("Request started | WorkflowId: {WorkflowId} | Jobs: {Count}", workflowId, jobs.Count);
        return workflowId;
    }

    /// <inheritdoc />
    public Task<PartialResult> StopAsync(string workflowId, CancellationToken ct = default)
    {
        return ControlAsync(workflowId, status => status is RequestStatus.Running or RequestStatus.Paused,
            id => workflowController.StopWorkflowAsync(id), ct);
    }

    /// <inheritdoc />
    public Task<PartialResult> PauseAsync(string workflowId, CancellationToken ct = default)
    {
        return ControlAsync(workflowId, status => status == RequestStatus.Running,
            workflowController.PauseWorkflowAsync, ct);
    }

    /// <inheritdoc />
    public Task<PartialResult> ResumeAsync(string workflowId, CancellationToken ct = default)
    {
        return ControlAsync(workflowId, status => status == RequestStatus.Paused,
            workflowController.ResumeWorkflowAsync, ct);
    }

    /// <inheritdoc />
    public async Task<int> StopAllAsync(CancellationToken ct = default)
    {
        var actives = await ListActiveAsync(includeLogs: false, ct).ConfigureAwait(false);
        var count = 0;
        foreach (var workflowId in actives.Keys)
            if ((await StopAsync(workflowId, ct).ConfigureAwait(false)).Succeeded > 0)
                count++;
        return count;
    }

    /// <inheritdoc />
    public async Task<int> PauseAllAsync(CancellationToken ct = default)
    {
        var actives = await ListActiveAsync(includeLogs: false, ct).ConfigureAwait(false);
        var count = 0;
        foreach (var workflowId in actives.Where(kv => kv.Value.Status == RequestStatus.Running).Select(kv => kv.Key))
            if ((await PauseAsync(workflowId, ct).ConfigureAwait(false)).Succeeded > 0)
                count++;
        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, Summary>> ListActiveAsync(bool includeLogs = true,
        CancellationToken ct = default)
    {
        var groups = await ListGroupsAsync(ct).ConfigureAwait(false);
        var paused = await GetPausedWorkflowIdsAsync(ct).ConfigureAwait(false);
        var active = groups.Where(kv =>
            DeriveStatus(kv.Value, paused.Contains(kv.Key) ? JobStatus.Paused : null) is RequestStatus.Running
                or RequestStatus.Paused);
        return await ToSummariesAsync(active, includeLogs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, Summary>> ListCompletedAsync(bool includeLogs = true,
        CancellationToken ct = default)
    {
        var groups = await ListGroupsAsync(ct).ConfigureAwait(false);
        var completed = groups.Where(kv =>
            DeriveStatus(kv.Value) is not (RequestStatus.Running or RequestStatus.Paused));
        return await ToSummariesAsync(completed, includeLogs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string workflowId, CancellationToken ct = default)
    {
        var jobs = await jobsRepository.GetByWorkflowIdAsync(workflowId, ct).ConfigureAwait(false);
        if (jobs.Count == 0)
        {
            logger.LogWarning("Request {WorkflowId} not found — delete skipped.", workflowId);
            return false;
        }

        var record = await workflowsRepository.GetAsync(workflowId, ct).ConfigureAwait(false);
        if (DeriveStatus(jobs, record?.Status) is RequestStatus.Running or RequestStatus.Paused)
            await workflowController.StopWorkflowAsync(workflowId, waitForExit: true).ConfigureAwait(false);

        await DeleteWorkflowAsync(workflowId, record, ct).ConfigureAwait(false);
        logger.LogInformation("Deleted request {WorkflowId} ({Count} job(s)).", workflowId, jobs.Count);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllCompletedAsync(CancellationToken ct = default)
    {
        var completed = await ListCompletedAsync(includeLogs: false, ct).ConfigureAwait(false);
        var deleted = 0;
        foreach (var workflowId in completed.Keys)
        {
            var record = await workflowsRepository.GetAsync(workflowId, ct).ConfigureAwait(false);
            await DeleteWorkflowAsync(workflowId, record, ct).ConfigureAwait(false);
            deleted++;
        }

        logger.LogInformation("Deleted {Count} completed/cancelled request(s).", deleted);
        return deleted;
    }

    /// <summary>
    ///     Plans one <see cref="PlannedJob" /> per job specification, in the same order — the single planner shared
    ///     by <see cref="PreviewAsync" /> and <see cref="CreateAsync" /> so both apply the identical input-problem and
    ///     output-path-conflict definition.
    /// </summary>
    private async Task<IReadOnlyList<PlannedJob>> PlanAsync(IReadOnlyList<JobSpecification> jobs)
    {
        var duplicatePaths = FindDuplicateOutputPath(jobs);
        var activeOutputPaths = await GetActiveOutputPathsAsync().ConfigureAwait(false);

        return jobs
            .Select(j => new PlannedJob(j.OutputPath, j.WorkbookPath, j.WorksheetName,
                duplicatePaths.Contains(j.OutputPath) ? ConflictKind.DuplicateWithinRequest
                : activeOutputPaths.Contains(j.OutputPath) ? ConflictKind.ConflictsWithActiveRequest
                : File.Exists(j.OutputPath) ? ConflictKind.OverwritesExistingFile
                : ConflictKind.None,
                FindProblem(j)))
            .ToList();
    }

    /// <summary>
    ///     Returns why <paramref name="job" /> cannot run as configured, or <see cref="JobProblem.None" />. Only
    ///     cheap checks: file existence and numeric sanity, never opening the workbook or presentation (a preview
    ///     must not block on a file held open by Excel).
    /// </summary>
    private static JobProblem FindProblem(JobSpecification job)
    {
        if (!File.Exists(job.WorkbookPath)) return JobProblem.WorkbookNotFound;
        if (!File.Exists(job.TemplatePresentationPath)) return JobProblem.TemplateNotFound;
        if (job.TemplateSlideIndex < 1) return JobProblem.InvalidTemplateSlide;

        return job.RowFilter switch
        {
            Recipe.Models.IndexRangeFilter { Start: < 1 } => JobProblem.InvalidRowFilter,
            Recipe.Models.IndexRangeFilter filter when filter.End < filter.Start => JobProblem.InvalidRowFilter,
            Recipe.Models.PartitionBlockFilter { IsValid: false } => JobProblem.InvalidRowFilter,
            _ => JobProblem.None
        };
    }

    /// <summary>The log file of workflow <paramref name="workflowId" />: named after the id (unique), never after the user-chosen request name.</summary>
    private static string ResolveWorkflowLogPath(string workflowId)
    {
        return Path.Combine(NameAndPaths.LogsFolder.WorkflowPath, $"{workflowId}.log");
    }

    private async Task DeleteWorkflowAsync(string workflowId, WorkflowRecord? record, CancellationToken ct)
    {
        await jobsRepository.DeleteByWorkflowIdAsync(workflowId, ct).ConfigureAwait(false);
        await workflowsRepository.DeleteAsync(workflowId, ct).ConfigureAwait(false);
        if (record is not null) DeleteLogFile(record.LogPath);
    }

    /// <summary>Removes a workflow's log file. Best-effort: the file may still be held open by the logger of a workflow that is still unwinding.</summary>
    private void DeleteLogFile(string logPath)
    {
        logPath = Path.GetFullPath(logPath); // CodeQL path-injection sanitizer — keep (see AGENTS.md Security Patterns)
        try
        {
            File.Delete(logPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to delete log file '{LogPath}'.", logPath);
        }
    }

    #region Aggregation helpers

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<JobSnapshot>>> ListGroupsAsync(
        CancellationToken ct = default)
    {
        var all = await jobsRepository.GetAllAsync(ct).ConfigureAwait(false);
        return all
            .GroupBy(j => j.WorkflowId)
            .ToDictionary(g => g.Key, IReadOnlyList<JobSnapshot> (g) => [.. g]);
    }

    private async Task<IReadOnlyDictionary<string, Summary>> ToSummariesAsync(
        IEnumerable<KeyValuePair<string, IReadOnlyList<JobSnapshot>>> groups, bool includeLogs, CancellationToken ct)
    {
        var result = new Dictionary<string, Summary>();
        foreach (var (workflowId, jobs) in groups)
        {
            var summary = await ToSummaryAsync(workflowId, jobs, includeLogs, ct).ConfigureAwait(false);
            if (summary != null) result[workflowId] = summary;
        }

        return result;
    }

    /// <summary>
    ///     Returns every output path in <paramref name="jobs" /> that is claimed by more than one job — i.e.
    ///     two Mappings sharing the same worksheet (see <see cref="BuildJobs" />). Empty when no job's output
    ///     path collides with another job's within the same request.
    /// </summary>
    internal static IReadOnlySet<string> FindDuplicateOutputPath(IReadOnlyList<JobSpecification> jobs)
    {
        return jobs
            .GroupBy(j => j.OutputPath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns the output paths claimed by every job of another active (running, pending, or paused)
    ///     request — shared by <see cref="FindConflictingOutputPathAsync" /> and <see cref="PreviewAsync" />
    ///     so both use the exact same conflict definition.
    /// </summary>
    private async Task<IReadOnlySet<string>> GetActiveOutputPathsAsync()
    {
        var active = await jobsRepository.GetNonTerminalAsync().ConfigureAwait(false);
        return active
            .Where(j => j.JobStatus is JobStatus.Running or JobStatus.Pending or JobStatus.Paused)
            .Select(j => j.OutputPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns the first output path among <paramref name="jobs" /> that collides with an output path
    ///     already claimed by a job of another active (running or paused) request.
    /// </summary>
    private async Task<string?> FindConflictingOutputPathAsync(IReadOnlyList<JobSpecification> jobs)
    {
        var activeOutputPaths = await GetActiveOutputPathsAsync().ConfigureAwait(false);
        return jobs.Select(j => j.OutputPath).FirstOrDefault(activeOutputPaths.Contains);
    }

    /// <summary>
    ///     Applies a workflow-wide control action when the workflow's current status makes it eligible, counting
    ///     it as skipped (not failed) otherwise — e.g. stopping a workflow that already finished.
    /// </summary>
    private async Task<PartialResult> ControlAsync(
        string workflowId, Func<RequestStatus, bool> eligible, Func<string, Task<bool>> action, CancellationToken ct)
    {
        var jobs = await jobsRepository.GetByWorkflowIdAsync(workflowId, ct).ConfigureAwait(false);
        var record = await workflowsRepository.GetAsync(workflowId, ct).ConfigureAwait(false);
        if (!eligible(DeriveStatus(jobs, record?.Status))) return new PartialResult(0, 1);

        return await action(workflowId).ConfigureAwait(false) ? new PartialResult(1, 0) : new PartialResult(0, 1);
    }

    /// <summary>
    ///     Flattens a recipe's <c>Mappings</c> into one <see cref="JobSpecification" /> per
    ///     (worksheet source × mapping) pair — every value already resolved, no id left to look up.
    /// </summary>
    internal static List<JobSpecification> BuildJobs(Recipe.Models.Recipe recipe, Request request)
    {
        return
        [
            .. recipe.Mappings.SelectMany(m => m.Sources.Select(s => new JobSpecification(
                s.Workbook.BookPath,
                s.Worksheet.SheetName,
                s.UsedColumns,
                s.RowFilter,
                m.Template.Presentation.PresentationPath,
                m.Template.Slide.SlideIndex,
                m.TextInstructions,
                m.ImageInstructions,
                BuildOutputPath(request, s.Workbook.BookPath, s.Worksheet.SheetName),
                request.AllowLocalPaths)))
        ];
    }

    private static string BuildOutputPath(Request request, string bookPath, string sheetName)
    {
        var bookName = Path.GetFileNameWithoutExtension(bookPath);
        var outputFileName = $"{Naming.SanitizeFileName(sheetName)}{request.OutputType.ToExtension()}";
        return Path.Combine(request.SaveFolder, bookName, outputFileName);
    }

    /// <summary>
    ///     Derives the request status from its jobs. A workflow stored as <see cref="JobStatus.Paused" /> turns
    ///     running jobs into <see cref="RequestStatus.Paused" />, since a workflow-level pause does not rewrite job rows.
    /// </summary>
    internal static RequestStatus DeriveStatus(IReadOnlyList<JobSnapshot> jobs, JobStatus? workflowStatus = null)
    {
        return RequestStatusRules.Derive([.. jobs.Select(j => j.JobStatus)], workflowStatus == JobStatus.Paused);
    }

    private async Task<IReadOnlySet<string>> GetPausedWorkflowIdsAsync(CancellationToken ct)
    {
        var nonTerminal = await workflowsRepository.GetNonTerminalAsync(ct).ConfigureAwait(false);
        return nonTerminal.Where(r => r.Status == JobStatus.Paused).Select(r => r.WorkflowId).ToHashSet();
    }

    private async Task<Summary?> ToSummaryAsync(string workflowId, IReadOnlyList<JobSnapshot> jobs, bool includeLogs,
        CancellationToken ct)
    {
        if (jobs.Count == 0) return null;
        var record = await workflowsRepository.GetAsync(workflowId, ct).ConfigureAwait(false);
        if (record is null) return null;

        var status = DeriveStatus(jobs, record.Status);
        var createdAt = record.CreatedAt;
        var completedAt = status is not (RequestStatus.Running or RequestStatus.Paused)
            ? jobs.Max(j => j.Timestamp)
            : (DateTimeOffset?)null;
        // Reading and regex-parsing a request's whole .log file is the expensive part of building a Summary —
        // skip it entirely for list views, which never display log content anyway (see IService.ListActiveAsync).
        IReadOnlyList<LogEntry> logEntries = includeLogs ? logFileReader.ReadAll(record.LogPath) : [];

        var jobSummaries = jobs.ToDictionary(j => j.JobId, j => ToJobSummary(workflowId, j, logEntries));

        return new Summary
        {
            Request = record.Request,
            Status = status,
            Phase = DeriveWorkflowPhase(jobs),
            CreatedAt = createdAt,
            CompletedAt = completedAt,
            Logs = [.. logEntries.Where(e => e.Path == workflowId)],
            Jobs = jobSummaries
        };
    }

    private static JobSummary ToJobSummary(string workflowId, JobSnapshot job, IReadOnlyList<LogEntry> logEntries)
    {
        var jobScopePrefix = $"{workflowId}/{job.JobId}";
        return new JobSummary
        {
            JobStatus = job.JobStatus,
            Phase = job.Phase,
            CurrentIndex = job.CurrentIndex,
            TotalRows = job.TotalRows,
            OutputPath = job.OutputPath,
            CompletedAt = job.JobStatus is JobStatus.Complete or JobStatus.Cancelled or JobStatus.Error
                ? job.Timestamp
                : null,
            Logs =
            [
                .. logEntries
                    .Where(e => e.Path == jobScopePrefix ||
                                e.Path.StartsWith(jobScopePrefix + "/", StringComparison.Ordinal))
            ]
        };
    }

    /// <summary>Derives the aggregate request phase purely from current job statuses — not persisted.</summary>
    private static WorkflowPhase DeriveWorkflowPhase(IReadOnlyList<JobSnapshot> jobs)
    {
        if (jobs.All(j => j.JobStatus == JobStatus.Pending)) return WorkflowPhase.PreparationStarted;
        if (jobs.All(j => j.JobStatus is JobStatus.Complete or JobStatus.Cancelled or JobStatus.Error))
            return WorkflowPhase.Completed;
        return WorkflowPhase.ProcessingStarted;
    }

    #endregion
}
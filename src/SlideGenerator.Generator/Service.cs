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
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Utilities;

namespace SlideGenerator.Generator;

/// <summary>
///     Defines the facade for workflow lifecycle operations exposed to the IPC layer.
///     Consumers (e.g. <c>GeneratingActiveHandler</c>) depend only on this interface and are
///     completely decoupled from <see cref="IWorkflowController" /> internals.
/// </summary>
public interface IService
{
    /// <summary>
    ///     Resumes any crashed non-terminal workflow and starts the underlying workflow runner.
    ///     Must be called once during application startup before any other method.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    ///     Stops the underlying workflow runner gracefully.
    ///     Must be called during application shutdown.
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Computes the workflow list <see cref="CreateAsync" /> would spawn for <paramref name="request" />,
    ///     without spawning anything — lets a caller (e.g. a run confirmation dialog) show exactly what will
    ///     be created and flag every output-path conflict <see cref="CreateAsync" /> would otherwise reject
    ///     at submit time, using the identical conflict definition. Does not itself throw on conflicts —
    ///     conflicts are reported per workflow via <see cref="PlannedWorkflow.ConflictKind" /> instead.
    /// </summary>
    Task<IReadOnlyList<PlannedWorkflow>> PreviewAsync(Request request, CancellationToken ct = default);

    /// <summary>
    ///     Starts a new slide-generation request for the given <paramref name="request" /> — spawns one
    ///     workflow per computed workflow specification. Multiple active requests for the same recipe are
    ///     allowed; throws if two of the request's own workflows would write to the same output path, or if a
    ///     computed workflow's output path collides with one already claimed by another active (running or
    ///     paused) request's workflow.
    /// </summary>
    /// <returns>The generated request id grouping every workflow spawned for it.</returns>
    Task<string> CreateAsync(Request request, CancellationToken ct = default);

    /// <summary>
    ///     Cancels every workflow belonging to <paramref name="requestId" /> that is still runnable or paused.
    ///     Best-effort — workflows already complete/terminated are counted as skipped.
    /// </summary>
    Task<PartialResult> StopAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Pauses every workflow belonging to <paramref name="requestId" /> that is still running.
    ///     Best-effort — workflows not currently running are counted as skipped.
    /// </summary>
    Task<PartialResult> PauseAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Resumes every workflow belonging to <paramref name="requestId" /> that is currently paused.
    ///     Best-effort — workflows not currently paused are counted as skipped.
    /// </summary>
    Task<PartialResult> ResumeAsync(string requestId, CancellationToken ct = default);

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
    ///     Returns summaries of all currently active (running or paused) requests, keyed by request id.
    /// </summary>
    /// <param name="includeLogs">
    ///     When <see langword="false" />, <see cref="Summary.Logs" />/<see cref="WorkflowSummary.Logs" /> are
    ///     empty and no request's log file is read from disk — pass <see langword="false" /> when only the
    ///     list itself is needed (e.g. rendering a list of requests), since every group would otherwise pay
    ///     for a full read-and-parse of its <c>.log</c> file even though the list view never shows it.
    /// </param>
    /// <param name="ct"></param>
    Task<IReadOnlyDictionary<string, Summary>> ListActiveAsync(bool includeLogs = true, CancellationToken ct = default);

    /// <summary>
    ///     Returns summaries of all completed, canceled, or errored requests, keyed by request id.
    /// </summary>
    /// <param name="includeLogs">See <see cref="ListActiveAsync" />.</param>
    /// <param name="ct"></param>
    Task<IReadOnlyDictionary<string, Summary>> ListCompletedAsync(bool includeLogs = true, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes every workflow belonging to a request. If the request is still
    ///     active (running or paused), it is stopped first.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the request was found and deleted;
    ///     <see langword="false" /> if not found.
    /// </returns>
    Task<bool> DeleteAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes all completed and canceled requests and their associated data.
    /// </summary>
    /// <returns>The number of requests deleted.</returns>
    Task<int> DeleteAllCompletedAsync(CancellationToken ct = default);
}

/// <summary>
///     Implements <see cref="IService" /> on top of <see cref="IWorkflowController" /> (plain C#/Task workflow execution
///     — WorkflowCore is gone). <c>CreateAsync</c>'s spawn phase (reading the recipe, computing the workflow
///     list) runs as plain code here, deliberately not itself throttled by <c>MaxConcurrentWorkflows</c> —
///     see <see cref="IWorkflowController" /> remarks.
/// </summary>
internal sealed class Service(
    IWorkflowController workflowController,
    IRecipeRepository recipeRepository,
    IRequestsRepository requestsRepository,
    IWorkflowsRepository workflowsRepository,
    IEventBus eventBus,
    ILogFileReader logFileReader,
    ILogger<Service> logger)
    : IService
{
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
    public async Task<IReadOnlyList<PlannedWorkflow>> PreviewAsync(Request request, CancellationToken ct = default)
    {
        var entry = await recipeRepository.GetAsync(request.RecipeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Recipe {request.RecipeId} not found.");

        var workflows = BuildWorkflows(entry.Recipe, request);
        var duplicatePaths = FindDuplicateOutputPath(workflows);
        var activeOutputPaths = await GetActiveOutputPathsAsync().ConfigureAwait(false);

        return workflows
            .Select(j => new PlannedWorkflow(j.OutputPath, j.WorkbookPath, j.WorksheetName,
                duplicatePaths.Contains(j.OutputPath) ? ConflictKind.DuplicateWithinRequest
                : activeOutputPaths.Contains(j.OutputPath) ? ConflictKind.ConflictsWithActiveRequest
                : ConflictKind.None))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<string> CreateAsync(Request request, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope("Start/{RecipeId}/{RequestName}", request.RecipeId, request.Name);

        var entry = await recipeRepository.GetAsync(request.RecipeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Recipe {request.RecipeId} not found.");

        var requestId = Guid.NewGuid().ToString();
        var logPath = ResolveWorkflowLogPath(request);
        var workflows = BuildWorkflows(entry.Recipe, request);

        var duplicatePaths = FindDuplicateOutputPath(workflows);
        if (duplicatePaths.Count > 0)
            throw new InvalidOperationException(
                $"Output path '{duplicatePaths.First()}' is used by more than one mapping in this recipe.");

        var conflictingPath = await FindConflictingOutputPathAsync(workflows).ConfigureAwait(false);
        if (conflictingPath is not null)
            throw new InvalidOperationException(
                $"Output path '{conflictingPath}' is already being generated by an active request.");

        await requestsRepository.CreateAsync(
            new RequestRecord(requestId, request, logPath, DateTimeOffset.UtcNow), ct).ConfigureAwait(false);

        eventBus.Publish(new RequestProgress
        {
            RequestId = requestId,
            Phase = RequestPhase.PreparationStarted,
            Timestamp = DateTimeOffset.UtcNow
        });
        eventBus.AnnounceExpectedWorkflowCount(requestId, workflows.Count);

        for (var workflowId = 0; workflowId < workflows.Count; workflowId++)
        {
            await workflowController.StartWorkflowAsync(requestId, workflowId, workflows[workflowId], logPath, ct).ConfigureAwait(false);
            logger.LogInformation("Workflow spawned | WorkflowId: {WorkflowId} | OutputPath: {OutputPath}", workflowId,
                workflows[workflowId].OutputPath);
        }

        logger.LogInformation("Request started | RequestId: {RequestId} | Workflows: {Count}", requestId, workflows.Count);
        return requestId;
    }

    /// <inheritdoc />
    public async Task<PartialResult> StopAsync(string requestId, CancellationToken ct = default)
    {
        var workflows = await workflowsRepository.GetByRequestIdAsync(requestId, ct).ConfigureAwait(false);
        return await FanOutAsync(workflows, j => j.WorkflowStatus is WorkflowStatus.Running or WorkflowStatus.Pending or WorkflowStatus.Paused,
            j => workflowController.StopWorkflowAsync(requestId, j.WorkflowId)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PartialResult> PauseAsync(string requestId, CancellationToken ct = default)
    {
        var workflows = await workflowsRepository.GetByRequestIdAsync(requestId, ct).ConfigureAwait(false);
        return await FanOutAsync(workflows, j => j.WorkflowStatus == WorkflowStatus.Running,
            j => workflowController.PauseWorkflowAsync(requestId, j.WorkflowId)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PartialResult> ResumeAsync(string requestId, CancellationToken ct = default)
    {
        var workflows = await workflowsRepository.GetByRequestIdAsync(requestId, ct).ConfigureAwait(false);
        return await FanOutAsync(workflows, j => j.WorkflowStatus == WorkflowStatus.Paused,
            j => workflowController.ResumeWorkflowAsync(requestId, j.WorkflowId)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> StopAllAsync(CancellationToken ct = default)
    {
        var actives = await ListActiveAsync(includeLogs: false, ct).ConfigureAwait(false);
        var count = 0;
        foreach (var requestId in actives.Keys)
            if ((await StopAsync(requestId, ct).ConfigureAwait(false)).Succeeded > 0)
                count++;
        return count;
    }

    /// <inheritdoc />
    public async Task<int> PauseAllAsync(CancellationToken ct = default)
    {
        var actives = await ListActiveAsync(includeLogs: false, ct).ConfigureAwait(false);
        var count = 0;
        foreach (var requestId in actives.Where(kv => kv.Value.WorkflowStatus == WorkflowStatus.Running).Select(kv => kv.Key))
            if ((await PauseAsync(requestId, ct).ConfigureAwait(false)).Succeeded > 0)
                count++;
        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, Summary>> ListActiveAsync(bool includeLogs = true,
        CancellationToken ct = default)
    {
        var groups = await ListGroupsAsync(ct).ConfigureAwait(false);
        var active = groups.Where(kv => DeriveStatus(kv.Value) is WorkflowStatus.Running or WorkflowStatus.Paused);
        return await ToSummariesAsync(active, includeLogs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, Summary>> ListCompletedAsync(bool includeLogs = true,
        CancellationToken ct = default)
    {
        var groups = await ListGroupsAsync(ct).ConfigureAwait(false);
        var completed = groups.Where(kv => DeriveStatus(kv.Value) is WorkflowStatus.Complete or WorkflowStatus.Cancelled);
        return await ToSummariesAsync(completed, includeLogs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string requestId, CancellationToken ct = default)
    {
        var workflows = await workflowsRepository.GetByRequestIdAsync(requestId, ct).ConfigureAwait(false);
        if (workflows.Count == 0)
        {
            logger.LogWarning("Request {RequestId} not found — delete skipped.", requestId);
            return false;
        }

        if (DeriveStatus(workflows) is WorkflowStatus.Running or WorkflowStatus.Paused)
            await StopAsync(requestId, ct).ConfigureAwait(false);

        await workflowsRepository.DeleteByRequestIdAsync(requestId, ct).ConfigureAwait(false);
        await requestsRepository.DeleteAsync(requestId, ct).ConfigureAwait(false);
        logger.LogInformation("Deleted request {RequestId} ({Count} workflow(s)).", requestId, workflows.Count);
        return true;
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllCompletedAsync(CancellationToken ct = default)
    {
        var completed = await ListCompletedAsync(includeLogs: false, ct).ConfigureAwait(false);
        var deleted = 0;
        foreach (var requestId in completed.Keys)
        {
            await workflowsRepository.DeleteByRequestIdAsync(requestId, ct).ConfigureAwait(false);
            await requestsRepository.DeleteAsync(requestId, ct).ConfigureAwait(false);
            deleted++;
        }

        logger.LogInformation("Deleted {Count} completed/cancelled request(s).", deleted);
        return deleted;
    }

    private static string ResolveWorkflowLogPath(Request request)
    {
        var fileName = Naming.SanitizeFileName(request.Name);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "workflow";
        return Path.Combine(NameAndPaths.LogsFolder.WorkflowPath, $"{fileName}.log");
    }

    #region Aggregation helpers

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<WorkflowSnapshot>>> ListGroupsAsync(
        CancellationToken ct = default)
    {
        var all = await workflowsRepository.GetAllAsync(ct).ConfigureAwait(false);
        return all
            .GroupBy(j => j.RequestId)
            .ToDictionary(g => g.Key, IReadOnlyList<WorkflowSnapshot> (g) => [.. g]);
    }

    private async Task<IReadOnlyDictionary<string, Summary>> ToSummariesAsync(
        IEnumerable<KeyValuePair<string, IReadOnlyList<WorkflowSnapshot>>> groups, bool includeLogs, CancellationToken ct)
    {
        var result = new Dictionary<string, Summary>();
        foreach (var (requestId, workflows) in groups)
        {
            var summary = await ToSummaryAsync(requestId, workflows, includeLogs, ct).ConfigureAwait(false);
            if (summary != null) result[requestId] = summary;
        }

        return result;
    }

    /// <summary>
    ///     Returns every output path in <paramref name="workflows" /> that is claimed by more than one workflow — i.e.
    ///     two Mappings sharing the same worksheet (see <see cref="BuildWorkflows" />). Empty when no workflow's output
    ///     path collides with another workflow's within the same request.
    /// </summary>
    internal static IReadOnlySet<string> FindDuplicateOutputPath(IReadOnlyList<WorkflowSpecification> workflows)
    {
        return workflows
            .GroupBy(j => j.OutputPath, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns the output paths claimed by every workflow of another active (running, pending, or paused)
    ///     request — shared by <see cref="FindConflictingOutputPathAsync" /> and <see cref="PreviewAsync" />
    ///     so both use the exact same conflict definition.
    /// </summary>
    private async Task<IReadOnlySet<string>> GetActiveOutputPathsAsync()
    {
        var active = await workflowsRepository.GetNonTerminalAsync().ConfigureAwait(false);
        return active
            .Where(j => j.WorkflowStatus is WorkflowStatus.Running or WorkflowStatus.Pending or WorkflowStatus.Paused)
            .Select(j => j.OutputPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Returns the first output path among <paramref name="workflows" /> that collides with an output path
    ///     already claimed by a workflow of another active (running or paused) request.
    /// </summary>
    private async Task<string?> FindConflictingOutputPathAsync(IReadOnlyList<WorkflowSpecification> workflows)
    {
        var activeOutputPaths = await GetActiveOutputPathsAsync().ConfigureAwait(false);
        return workflows.Select(j => j.OutputPath).FirstOrDefault(activeOutputPaths.Contains);
    }

    private static async Task<PartialResult> FanOutAsync(
        IReadOnlyList<WorkflowSnapshot> workflows, Func<WorkflowSnapshot, bool> eligible, Func<WorkflowSnapshot, Task<bool>> action)
    {
        var succeeded = 0;
        var skipped = 0;
        foreach (var workflow in workflows)
        {
            if (!eligible(workflow))
            {
                skipped++;
                continue;
            }

            if (await action(workflow).ConfigureAwait(false)) succeeded++;
            else skipped++;
        }

        return new PartialResult(succeeded, skipped);
    }

    /// <summary>
    ///     Flattens a recipe's <c>Mappings</c> into one <see cref="WorkflowSpecification" /> per
    ///     (worksheet source × mapping) pair — every value already resolved, no id left to look up.
    /// </summary>
    internal static List<WorkflowSpecification> BuildWorkflows(Recipe.Models.Recipe recipe, Request request)
    {
        return
        [
            .. recipe.Mappings.SelectMany(m => m.Sources.Select(s => new WorkflowSpecification(
                s.Workbook.BookPath,
                s.Worksheet.SheetName,
                s.UsedColumns,
                s.RowFilter,
                m.Template.Presentation.PresentationPath,
                m.Template.Slide.SlideIndex,
                m.TextInstructions,
                m.ImageInstructions,
                BuildOutputPath(request, s.Workbook.BookPath, s.Worksheet.SheetName))))
        ];
    }

    private static string BuildOutputPath(Request request, string bookPath, string sheetName)
    {
        var bookName = Path.GetFileNameWithoutExtension(bookPath);
        var outputFileName = $"{Naming.SanitizeFileName(sheetName)}{request.OutputType.ToExtension()}";
        return Path.Combine(request.SaveFolder, bookName, outputFileName);
    }

    /// <summary>
    ///     Aggregates a request's workflow statuses into one request-level <see cref="WorkflowStatus" />. Any workflow still
    ///     <see cref="WorkflowStatus.Pending" /> or <see cref="WorkflowStatus.Running" /> counts as the request Running
    ///     (a request with only just-spawned Pending workflows is "in progress", not idle) — this is the fix
    ///     for the gap where an all-Pending request used to fall through to Complete.
    /// </summary>
    internal static WorkflowStatus DeriveStatus(IReadOnlyList<WorkflowSnapshot> workflows)
    {
        if (workflows.Count == 0) return WorkflowStatus.Complete;
        if (workflows.Any(j => j.WorkflowStatus is WorkflowStatus.Running or WorkflowStatus.Pending)) return WorkflowStatus.Running;
        if (workflows.Any(j => j.WorkflowStatus == WorkflowStatus.Paused)) return WorkflowStatus.Paused;
        if (workflows.All(j => j.WorkflowStatus == WorkflowStatus.Cancelled)) return WorkflowStatus.Cancelled;
        return WorkflowStatus.Complete;
    }

    private async Task<Summary?> ToSummaryAsync(string requestId, IReadOnlyList<WorkflowSnapshot> workflows, bool includeLogs,
        CancellationToken ct)
    {
        if (workflows.Count == 0) return null;
        var record = await requestsRepository.GetAsync(requestId, ct).ConfigureAwait(false);
        if (record is null) return null;

        var status = DeriveStatus(workflows);
        var createdAt = record.CreatedAt;
        var completedAt = status is WorkflowStatus.Complete or WorkflowStatus.Cancelled
            ? workflows.Max(j => j.Timestamp)
            : (DateTimeOffset?)null;
        // Reading and regex-parsing a request's whole .log file is the expensive part of building a Summary —
        // skip it entirely for list views, which never display log content anyway (see IService.ListActiveAsync).
        IReadOnlyList<LogEntry> logEntries = includeLogs ? logFileReader.ReadAll(record.LogPath) : [];

        var workflowSummaries = workflows.ToDictionary(j => j.WorkflowId, j => ToWorkflowSummary(requestId, j, logEntries));

        return new Summary
        {
            Request = record.Request,
            WorkflowStatus = status,
            Phase = DeriveRequestPhase(workflows),
            CreatedAt = createdAt,
            CompletedAt = completedAt,
            Logs = [.. logEntries.Where(e => e.Path == requestId)],
            Workflows = workflowSummaries
        };
    }

    private static WorkflowSummary ToWorkflowSummary(string requestId, WorkflowSnapshot workflow, IReadOnlyList<LogEntry> logEntries)
    {
        var workflowScopePrefix = $"{requestId}/{workflow.WorkflowId}";
        return new WorkflowSummary
        {
            WorkflowStatus = workflow.WorkflowStatus,
            Phase = workflow.Phase,
            CurrentIndex = workflow.CurrentIndex,
            TotalRows = workflow.TotalRows,
            OutputPath = workflow.OutputPath,
            CompletedAt = workflow.WorkflowStatus is WorkflowStatus.Complete or WorkflowStatus.Cancelled or WorkflowStatus.Error
                ? workflow.Timestamp
                : null,
            Logs =
            [
                .. logEntries
                    .Where(e => e.Path == workflowScopePrefix ||
                                e.Path.StartsWith(workflowScopePrefix + "/", StringComparison.Ordinal))
            ]
        };
    }

    /// <summary>Derives the aggregate request phase purely from current workflow statuses — not persisted.</summary>
    private static RequestPhase DeriveRequestPhase(IReadOnlyList<WorkflowSnapshot> workflows)
    {
        if (workflows.All(j => j.WorkflowStatus == WorkflowStatus.Pending)) return RequestPhase.PreparationStarted;
        if (workflows.All(j => j.WorkflowStatus is WorkflowStatus.Complete or WorkflowStatus.Cancelled or WorkflowStatus.Error))
            return RequestPhase.Completed;
        return RequestPhase.ProcessingStarted;
    }

    #endregion
}
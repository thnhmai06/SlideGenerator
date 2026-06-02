/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Workflows;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Utilities;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.LifeCycleEvents;

namespace SlideGenerator.Generator.Infrastructure.Services;

/// <summary>
///     Implements <see cref="IGeneratingService" /> by wrapping the WorkflowCore
///     <see cref="IWorkflowHost" /> and <see cref="IWorkflowController" />.
///     Lives in Infrastructure because it has a direct dependency on the WorkflowCore framework.
/// </summary>
internal sealed class GeneratingService(
    IWorkflowHost workflowHost,
    IWorkflowController workflowController,
    IPersistenceProvider persistence,
    IGeneratingEventBus eventBus,
    IRecipeRepository recipeRepository,
    ILogger<GeneratingService> logger)
    : IGeneratingService
{
    private bool _isStarted;
    private LifeCycleEventHandler? _lifeCycleHandler;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        workflowHost.RegisterWorkflow<GeneratingWorkflow, GeneratingContext>();

        _lifeCycleHandler = HandleLifeCycleEvent;
        workflowHost.OnLifeCycleEvent += _lifeCycleHandler;

        await workflowHost.StartAsync(ct).ConfigureAwait(false);
        _isStarted = true;
        logger.LogInformation("WorkflowCore host started and GeneratingWorkflow registered.");
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        if (!_isStarted)
        {
            logger.LogDebug("WorkflowCore host was not started; skipping stop.");
            return;
        }

        if (_lifeCycleHandler is not null)
            workflowHost.OnLifeCycleEvent -= _lifeCycleHandler;

        await workflowHost.StopAsync(ct).ConfigureAwait(false);
        _isStarted = false;
        logger.LogInformation("WorkflowCore host stopped.");
    }

    /// <inheritdoc />
    public async Task<string> StartAsync(GeneratingRequest request, CancellationToken ct = default)
    {
        using var scope = logger.BeginScope("Start/{RecipeId}/{RequestName}", request.RecipeId, request.Name);

        _ = await recipeRepository.GetByIdAsync(request.RecipeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Recipe {request.RecipeId} not found.");

        var context = new GeneratingContext
        {
            Request = request,
            WorkflowLogPath = ResolveWorkflowLogPath(request),
            WorkflowScope = ResolveWorkflowScope(request)
        };
        var instanceId = await workflowHost
            .StartWorkflow(nameof(GeneratingWorkflow), 1, context)
            .ConfigureAwait(false);

        logger.LogInformation("Workflow started | InstanceId: {InstanceId}", instanceId);
        return instanceId;
    }

    /// <inheritdoc />
    public async Task<bool> CancelAsync(string instanceId, CancellationToken ct = default)
    {
        var success = await workflowController
            .TerminateWorkflow(instanceId)
            .ConfigureAwait(false);

        if (success)
            logger.LogInformation("Cancelled workflow {InstanceId}.", instanceId);
        else
            logger.LogWarning("Failed to cancel workflow {InstanceId} - may not be running.", instanceId);

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> PauseAsync(string instanceId, CancellationToken ct = default)
    {
        var success = await workflowController
            .SuspendWorkflow(instanceId)
            .ConfigureAwait(false);

        if (success)
            logger.LogInformation("Paused workflow {InstanceId}.", instanceId);
        else
            logger.LogWarning("Failed to pause workflow {InstanceId}.", instanceId);

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> ResumeAsync(string instanceId, CancellationToken ct = default)
    {
        var success = await workflowController
            .ResumeWorkflow(instanceId)
            .ConfigureAwait(false);

        if (success)
            logger.LogInformation("Resumed workflow {InstanceId}.", instanceId);
        else
            logger.LogWarning("Failed to resume workflow {InstanceId}.", instanceId);

        return success;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeneratingSummary>> ListActiveAsync(CancellationToken ct = default)
    {
        var runnable = await persistence
            .GetWorkflowInstances(WorkflowStatus.Runnable, nameof(GeneratingWorkflow), null, null, 0, int.MaxValue)
            .ConfigureAwait(false);
        var suspended = await persistence
            .GetWorkflowInstances(WorkflowStatus.Suspended, nameof(GeneratingWorkflow), null, null, 0, int.MaxValue)
            .ConfigureAwait(false);
        return runnable.Concat(suspended).Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeneratingSummary>> ListCompletedAsync(CancellationToken ct = default)
    {
        var complete = await persistence
            .GetWorkflowInstances(WorkflowStatus.Complete, nameof(GeneratingWorkflow), null, null, 0, int.MaxValue)
            .ConfigureAwait(false);
        var terminated = await persistence
            .GetWorkflowInstances(WorkflowStatus.Terminated, nameof(GeneratingWorkflow), null, null, 0, int.MaxValue)
            .ConfigureAwait(false);
        return complete.Concat(terminated).Select(ToSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<GeneratingSummary?> QueryAsync(string instanceId, CancellationToken ct = default)
    {
        var instance = await persistence.GetWorkflowInstance(instanceId, ct).ConfigureAwait(false);
        return instance is null ? null : ToSummary(instance);
    }

    /// <inheritdoc />
    public async Task<bool> IsRecipeInUseAsync(int recipeId, CancellationToken ct = default)
    {
        var active = await ListActiveAsync(ct).ConfigureAwait(false);
        return active.Any(s => s.RecipeId == recipeId);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string instanceId, CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(NameAndPaths.DataFolder.WorkflowsFile.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();
        try
        {
            await DeletePointerChildrenAsync(conn, tx, "\"WorkflowInstanceId\" = @id", instanceId, ct)
                .ConfigureAwait(false);
            await DeletePointersAsync(conn, tx, "\"WorkflowInstanceId\" = @id", instanceId, ct).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                $"DELETE FROM \"Workflow\" WHERE \"Id\" = @id AND \"Status\" IN ({(int)WorkflowStatus.Complete}, {(int)WorkflowStatus.Terminated})";
            cmd.Parameters.AddWithValue("@id", instanceId);
            var rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            tx.Commit();

            if (rows > 0) logger.LogInformation("Deleted completed workflow {InstanceId}.", instanceId);
            else logger.LogWarning("Workflow {InstanceId} not found or still active — delete skipped.", instanceId);
            return rows > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete workflow | Reason: DB transaction error | InstanceId: {InstanceId}",
                instanceId);
            tx.Rollback();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllCompletedAsync(CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(NameAndPaths.DataFolder.WorkflowsFile.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();
        try
        {
            const string completedFilter =
                "\"WorkflowInstanceId\" IN (SELECT \"Id\" FROM \"Workflow\" WHERE \"Status\" IN (2, 3))";
            await DeletePointerChildrenAsync(conn, tx, completedFilter, null, ct).ConfigureAwait(false);
            await DeletePointersAsync(conn, tx, completedFilter, null, ct).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                $"DELETE FROM \"Workflow\" WHERE \"Status\" IN ({(int)WorkflowStatus.Complete}, {(int)WorkflowStatus.Terminated})";
            var rows = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            tx.Commit();
            logger.LogInformation("Deleted {Count} completed/cancelled workflow(s).", rows);
            return rows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete completed workflows | Reason: DB transaction error");
            tx.Rollback();
            throw;
        }
    }

    private void HandleLifeCycleEvent(LifeCycleEvent e)
    {
        switch (e)
        {
            case WorkflowStarted ws:
                eventBus.Publish(new GeneratingProgress
                {
                    WorkflowInstanceId = ws.WorkflowInstanceId,
                    Event = GeneratingEvent.WorkflowStarted,
                    Status = GeneratingStatus.Running,
                    Timestamp = DateTimeOffset.UtcNow
                });
                break;
            case WorkflowCompleted wc:
                eventBus.Publish(new GeneratingProgress
                {
                    WorkflowInstanceId = wc.WorkflowInstanceId,
                    Event = GeneratingEvent.WorkflowCompleted,
                    Status = GeneratingStatus.Complete,
                    Timestamp = DateTimeOffset.UtcNow
                });
                break;
            case WorkflowError we:
                eventBus.Publish(new GeneratingProgress
                {
                    WorkflowInstanceId = we.WorkflowInstanceId,
                    Event = GeneratingEvent.WorkflowError,
                    Status = GeneratingStatus.Error,
                    Timestamp = DateTimeOffset.UtcNow
                });
                break;
        }
    }

    private static async Task DeletePointerChildrenAsync(
        SqliteConnection conn, SqliteTransaction tx,
        string pointerWhere, string? paramValue, CancellationToken ct)
    {
        foreach (var table in new[] { "ExtensionAttribute", "ExecutionError" })
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                $"DELETE FROM \"{table}\" WHERE \"ExecutionPointerId\" IN " +
                $"(SELECT \"Id\" FROM \"ExecutionPointer\" WHERE {pointerWhere})";
            if (paramValue is not null) cmd.Parameters.AddWithValue("@id", paramValue);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    private static async Task DeletePointersAsync(
        SqliteConnection conn, SqliteTransaction tx,
        string where, string? paramValue, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"DELETE FROM \"ExecutionPointer\" WHERE {where}";
        if (paramValue is not null) cmd.Parameters.AddWithValue("@id", paramValue);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static GeneratingSummary ToSummary(WorkflowInstance instance)
    {
        var status = instance.Status switch
        {
            WorkflowStatus.Runnable => GeneratingStatus.Running,
            WorkflowStatus.Suspended => GeneratingStatus.Paused,
            WorkflowStatus.Complete => GeneratingStatus.Complete,
            WorkflowStatus.Terminated => GeneratingStatus.Cancelled,
            _ => GeneratingStatus.Running
        };
        var ctx = instance.Data as GeneratingContext;
        return new GeneratingSummary
        {
            InstanceId = instance.Id,
            Name = ctx?.Request.Name,
            RecipeId = ctx?.Request.RecipeId ?? 0,
            Status = status,
            CreatedAt = new DateTimeOffset(instance.CreateTime, TimeSpan.Zero),
            CompletedAt = instance.CompleteTime.HasValue
                ? new DateTimeOffset(instance.CompleteTime.Value, TimeSpan.Zero)
                : null
        };
    }

    private static string ResolveWorkflowLogPath(GeneratingRequest request)
    {
        var fileName = Naming.SanitizeFileName(request.Name);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "workflow";
        return Path.Combine(NameAndPaths.LogsFolder.WorkflowPath, $"{fileName}.log");
    }

    private static string ResolveWorkflowScope(GeneratingRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Name) ? "Workflow" : request.Name;
    }
}
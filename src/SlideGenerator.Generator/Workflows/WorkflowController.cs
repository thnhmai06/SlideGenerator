/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowController.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Workflows.Runners;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Workflows.Engine;
using SlideGenerator.Logging.FileLogging;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Runs workflows by delegating to <see cref="IWorkflowEngine{TKey,TState}" /> (the generic scheduler/lifecycle
///     engine, in <c>SlideGenerator.Workflows</c>) with a <see cref="SlideGenerationRunner" /> (the 4-phase
///     slide-generation logic) wrapped in a <see cref="LoggingRunner" />. This type itself is a thin
///     adapter — its public contract is unchanged from before the Engine/Runner split, so
///     <c>Service</c>/<c>Program.Startup.cs</c>/existing tests mocking <see cref="IWorkflowController" /> don't need
///     to change.
/// </summary>
public interface IWorkflowController
{
    /// <summary>Resumes any non-terminal workflow found in storage (crash recovery). Call once at startup.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>Requests cancellation of every running workflow and waits for them to unwind. Call at shutdown.</summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Registers and starts a fresh workflow. Persists its initial <see cref="WorkflowSnapshot" /> (flushed
    ///     immediately, not on the next tick — so it is visible to conflict checks on newly created
    ///     requests right away) before starting execution, then returns without waiting for it to finish.
    /// </summary>
    Task StartWorkflowAsync(string requestId, int workflowId, WorkflowSpecification spec, string logPath,
        CancellationToken ct = default);

    /// <summary>
    ///     Pauses a running workflow at its next row/phase checkpoint. Returns <see langword="false" /> if not currently
    ///     running.
    /// </summary>
    Task<bool> PauseWorkflowAsync(string requestId, int workflowId);

    /// <summary>Resumes a paused workflow. Returns <see langword="false" /> if not currently paused.</summary>
    Task<bool> ResumeWorkflowAsync(string requestId, int workflowId);

    /// <summary>Cancels a running or paused workflow. Returns <see langword="false" /> if already terminal.</summary>
    Task<bool> StopWorkflowAsync(string requestId, int workflowId);
}

/// <inheritdoc cref="IWorkflowController" />
internal sealed class WorkflowController(
    IWorkflowEngine<WorkflowKey, WorkflowSnapshot> engine,
    IWorkflowResumeSource<WorkflowKey, WorkflowSnapshot> resumeSource,
    SlideGenerationRunner slideGenerationRunner,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier,
    IWorkflowsRepository workflowsRepository) : IWorkflowController
{
    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken ct = default)
    {
        return engine.InitializeAsync(resumeSource, ct);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        // Ordering matters: the engine guarantees no runner is still calling ReportAsync once
        // ShutdownAsync returns, so it is safe to flush right after.
        await engine.ShutdownAsync(ct).ConfigureAwait(false);
        await workflowsRepository.FlushAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StartWorkflowAsync(
        string requestId, int workflowId, WorkflowSpecification spec, string logPath, CancellationToken ct = default)
    {
        var initial = new WorkflowSnapshot(requestId, workflowId, WorkflowStatus.Pending, WorkflowPhase.CreatingOutput, 0, spec,
            DateTimeOffset.UtcNow);
        workflowsRepository.Enqueue(initial);
        await workflowsRepository.FlushAsync(ct).ConfigureAwait(false);

        var runner = new LoggingRunner(logPath, slideGenerationRunner, fileLoggerFactory, logNotifier);
        await engine.StartWorkflowAsync((requestId, workflowId), initial, runner, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> PauseWorkflowAsync(string requestId, int workflowId)
    {
        return engine.PauseAsync((requestId, workflowId));
    }

    /// <inheritdoc />
    public Task<bool> ResumeWorkflowAsync(string requestId, int workflowId)
    {
        return engine.ResumeAsync((requestId, workflowId));
    }

    /// <inheritdoc />
    public Task<bool> StopWorkflowAsync(string requestId, int workflowId)
    {
        return engine.StopAsync((requestId, workflowId));
    }
}
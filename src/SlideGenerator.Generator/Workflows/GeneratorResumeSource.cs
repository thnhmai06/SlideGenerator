/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratorResumeSource.cs
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
using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Finds workflows left non-terminal by a crash and rebuilds everything <see cref="IWorkflowEngine{TKey,TState}" />
///     needs to resume them — including a real per-request <see cref="LoggingRunner" /> log path read
///     from <see cref="IRequestsRepository" />, fixing the previous behavior of always falling back to a
///     synthetic <c>{requestId}.log</c> path on resume regardless of what the request actually used.
/// </summary>
internal sealed class GeneratorResumeSource(
    IWorkflowsRepository workflowsRepository,
    IRequestsRepository requestsRepository,
    SlideGenerationRunner slideGenerationRunner,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier) : IWorkflowResumeSource<WorkflowKey, WorkflowSnapshot>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingWorkflow<WorkflowKey, WorkflowSnapshot>>> GetPendingWorkflowsAsync(CancellationToken ct)
    {
        var nonTerminal = await workflowsRepository.GetNonTerminalAsync(ct).ConfigureAwait(false);
        var result = new List<PendingWorkflow<WorkflowKey, WorkflowSnapshot>>(nonTerminal.Count);
        foreach (var snapshot in nonTerminal)
        {
            var request = await requestsRepository.GetAsync(snapshot.RequestId, ct).ConfigureAwait(false);
            var logPath = request?.LogPath ?? DefaultLogPath(snapshot.RequestId);
            var runner = new LoggingRunner(logPath, slideGenerationRunner, fileLoggerFactory, logNotifier);
            result.Add(new PendingWorkflow<WorkflowKey, WorkflowSnapshot>((snapshot.RequestId, snapshot.WorkflowId), snapshot, runner));
        }

        return result;
    }

    private static string DefaultLogPath(string requestId)
    {
        return Path.Combine(NameAndPaths.LogsFolder.WorkflowPath, $"{requestId}.log");
    }
}
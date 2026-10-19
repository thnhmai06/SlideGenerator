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

using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Logging.FileLogging;
using SlideGenerator.Workflows.Engine;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Finds workflows left non-terminal by a crash and rebuilds everything
///     <see cref="IWorkflowEngine{TKey,TState}" /> needs to resume them — including a <see cref="LoggingWorkflowRunner" />
///     writing to the same log file the original run used, read from the persisted <see cref="WorkflowRecord" />.
///     Each workflow's own unfinished jobs are re-read by <see cref="SlideGenerationWorkflow" /> when it runs.
/// </summary>
internal sealed class GeneratorResumeSource(
    IWorkflowsRepository workflowsRepository,
    SlideGenerationWorkflow slideGenerationWorkflow,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier) : IWorkflowResumeSource<string, WorkflowSnapshot>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingWorkflow<string, WorkflowSnapshot>>> GetPendingWorkflowsAsync(
        CancellationToken ct)
    {
        var nonTerminal = await workflowsRepository.GetNonTerminalAsync(ct).ConfigureAwait(false);
        var result = new List<PendingWorkflow<string, WorkflowSnapshot>>(nonTerminal.Count);
        foreach (var record in nonTerminal)
        {
            // A workflow that was Paused before the crash resumes as plain Running — there is no persisted "why".
            var state = new WorkflowSnapshot(record.WorkflowId, JobStatus.Running, DateTimeOffset.UtcNow);
            var runner = new LoggingWorkflowRunner(record.LogPath, slideGenerationWorkflow, fileLoggerFactory, logNotifier);
            result.Add(new PendingWorkflow<string, WorkflowSnapshot>(record.WorkflowId, state, runner));
        }

        return result;
    }
}

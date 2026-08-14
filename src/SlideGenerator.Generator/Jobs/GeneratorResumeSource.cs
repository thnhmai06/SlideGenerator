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
using SlideGenerator.Generator.Jobs.Workloads;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Jobs.Engine;
using SlideGenerator.Logging.FileLogging;
using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     Finds jobs left non-terminal by a crash and rebuilds everything <see cref="IJobEngine{TKey,TState}" />
///     needs to resume them — including a real per-request <see cref="LoggingWorkload" /> log path read
///     from <see cref="IRequestsRepository" />, fixing the previous behavior of always falling back to a
///     synthetic <c>{requestId}.log</c> path on resume regardless of what the request actually used.
/// </summary>
internal sealed class GeneratorResumeSource(
    IJobsRepository jobsRepository,
    IRequestsRepository requestsRepository,
    SlideGenerationWorkload slideGenerationWorkload,
    IFileLoggerFactory fileLoggerFactory,
    ILogNotifier logNotifier) : IJobResumeSource<JobKey, JobSnapshot>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingJob<JobKey, JobSnapshot>>> GetPendingJobsAsync(CancellationToken ct)
    {
        var nonTerminal = await jobsRepository.GetNonTerminalAsync(ct).ConfigureAwait(false);
        var result = new List<PendingJob<JobKey, JobSnapshot>>(nonTerminal.Count);
        foreach (var snapshot in nonTerminal)
        {
            var request = await requestsRepository.GetAsync(snapshot.RequestId, ct).ConfigureAwait(false);
            var logPath = request?.LogPath ?? DefaultLogPath(snapshot.RequestId);
            var workload = new LoggingWorkload(logPath, slideGenerationWorkload, fileLoggerFactory, logNotifier);
            result.Add(new PendingJob<JobKey, JobSnapshot>((snapshot.RequestId, snapshot.JobId), snapshot, workload));
        }

        return result;
    }

    private static string DefaultLogPath(string requestId)
    {
        return Path.Combine(NameAndPaths.LogsFolder.WorkflowPath, $"{requestId}.log");
    }
}
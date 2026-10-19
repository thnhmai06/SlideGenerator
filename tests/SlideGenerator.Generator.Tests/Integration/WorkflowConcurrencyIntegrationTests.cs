/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: WorkflowConcurrencyIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Workflows.Engine;
using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     End-to-end Request -&gt; Workflow -&gt; Job scenario (a): two workflows each holding three jobs run
///     concurrently through the real <see cref="SlideGenerator.Generator.Workflows.SlideGenerationWorkflow" />,
///     the real <see cref="SlideGenerator.Generator.Workflows.WorkflowController" />, the real
///     <see cref="WorkflowEngine{TKey,TState}" /> (single instance shared by both workflows), the real
///     <see cref="JobGate" /> and the real <see cref="JobExecutor" /> against the Data.xlsx / Template.pptx
///     fixtures. Asserts that the shared job cap
///     (<see cref="Setting.PerformanceSetting.MaxConcurrentJobs" />) is enforced across workflows, not just within
///     one, and that every job still reaches <see cref="JobStatus.Complete" />.
/// </summary>
public sealed class WorkflowConcurrencyIntegrationTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     With <see cref="Setting.PerformanceSetting.MaxConcurrentJobs" /> set to 2 and 2 workflows x 3 jobs
    ///     (6 jobs in total), the number of jobs simultaneously inside <see cref="JobExecutor" /> must never
    ///     exceed 2 across both workflows combined; every one of the 6 jobs must still reach
    ///     <see cref="JobStatus.Complete" />.
    /// </summary>
    /// <remarks>
    ///     The "concurrent jobs" the assertion counts are derived from the publish-only
    ///     <see cref="IEventBus" />: <see cref="JobExecutor.AdvancePhaseAsync" /> emits
    ///     <see cref="JobStatus.Running" /> at every durable phase transition; <see cref="JobExecutor" />'s
    ///     row-loop emits <see cref="JobStatus.Running" /> per row (non-durable); the workflow emits the
    ///     terminal <see cref="JobStatus.Complete" /> when a job finishes. So for every snapshot published
    ///     at time <c>T</c>, the set of <c>(workflowId, jobId)</c> pairs whose most-recent status is
    ///     <c>Running</c> at time <c>T</c> is exactly the count of jobs concurrently inside the executor.
    /// </remarks>
    [Fact(DisplayName = "INTEGRATION: (a) cross-workflow MaxConcurrentJobs cap")]
    public async Task TwoWorkflowsThreeJobs_MaxConcurrentJobsTwo_NeverExceedsTwoRunningJobsAndAllComplete()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        await using var harness = new WorkflowTestHarness();
        harness.Settings.MaxConcurrentJobs = 2;

        // Start 2 workflows of 3 jobs each. Use IndexRangeFilter(1,3) to keep the test fast — 3 rows per
        // job is enough to expose any concurrency overshoot (any extra slot would be visible).
        var workflowIds = new List<string>(2);
        for (var w = 0; w < 2; w++)
        {
            var specs = new List<JobSpecification>(3);
            for (var j = 0; j < 3; j++)
            {
                specs.Add(harness.BuildSpec(rowRange: new IndexRangeFilter(1, 3)));
            }

            workflowIds.Add(await harness.StartAsync(specs, name: $"wf-{w}", TestContext.Current.CancellationToken));
        }

        // Wait for both workflows to reach their terminal Completed state.
        foreach (var workflowId in workflowIds)
        {
            var terminal = await harness.Observer.WaitTerminalAsync(workflowId)
                .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            if (terminal.Outcome != WorkflowOutcome.Completed)
            {
                // Surface the underlying failure: dump captured log entries and the terminal exception.
                var logDump = string.Join("\n",
                    harness.LogNotifier.Entries
                        .Where(e => e.Path.StartsWith(workflowId))
                        .Select(e => $"  [{e.Level}] {e.Path} :: {e.Info}"));
                throw new InvalidOperationException(
                    $"Workflow {workflowId} faulted. Terminal exception: " +
                    $"{terminal.Exception?.GetType().Name}: {terminal.Exception?.Message}\n" +
                    $"Stack: {terminal.Exception?.StackTrace}\nLog entries:\n{logDump}");
            }

            terminal.Outcome.Should().Be(WorkflowOutcome.Completed,
                $"workflow {workflowId} should complete normally (max concurrent jobs = 2)");
        }

        // From the publish-order event timeline, compute the peak concurrent "Running" jobs. We replay
        // snapshots in order; at every step, the count of (workflowId, jobId) pairs whose most-recent
        // status is Running is the count of jobs simultaneously inside JobExecutor.
        // Enumerated in publish order (the event bus is a ConcurrentQueue); Timestamp is the snapshot
        // creation time and is not a valid ordering because concurrent writers can publish out of order.
        var timeline = harness.EventBus.Jobs.ToList();
        var perJobLatest = new Dictionary<string, JobStatus>();
        var maxObserved = 0;
        foreach (var snap in timeline)
        {
            var key = $"{snap.WorkflowId}/{snap.JobId}";
            perJobLatest[key] = snap.JobStatus;
            var currentRunning = perJobLatest.Values.Count(s => s == JobStatus.Running);
            if (currentRunning > maxObserved)
            {
                maxObserved = currentRunning;
            }
        }

        maxObserved.Should().BeLessThanOrEqualTo(2,
            $"MaxConcurrentJobs=2 must never be exceeded across both workflows; observed peak={maxObserved}");

        // Every job reaches Complete.
        foreach (var workflowId in workflowIds)
        {
            var jobs = await harness.Jobs.GetByWorkflowIdAsync(workflowId, TestContext.Current.CancellationToken);
            jobs.Should().HaveCount(3);

            // Surface why a job failed: its exception is logged under the workflow's scope path.
            var errorLog = "";
            if (jobs.Any(j => j.JobStatus != JobStatus.Complete))
            {
                // The workflow's .log file also carries the exception text, which LogEntry does not.
                using var stream = new FileStream(Path.Combine(harness.TempRoot, $"{workflowId}.log"),
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                errorLog = reader.ReadToEnd();
            }

            jobs.Should().OnlyContain(j => j.JobStatus == JobStatus.Complete,
                $"every job of workflow {workflowId} must reach Complete despite the cap. Log:\n{errorLog}");
            jobs.Should().OnlyContain(j => j.Phase == JobPhase.Done);
        }
    }
}
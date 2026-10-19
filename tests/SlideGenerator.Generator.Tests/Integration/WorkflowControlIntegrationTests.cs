/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: WorkflowControlIntegrationTests.cs
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
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Workflows.Engine;
using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     End-to-end Request -&gt; Workflow -&gt; Job scenario (b): pause/resume/stop acting on the whole
///     workflow through the real <see cref="WorkflowController" />, the real
///     <see cref="WorkflowEngine{TKey,TState}" /> and the real <see cref="PauseGate" />. Verifies that the
///     cooperative checkpoint halts progress between rows (not mid-row), that a job still queued for a
///     <see cref="JobGate" /> slot does not actually start while paused, and that a Stop while paused exits
///     promptly with every job row ending <see cref="JobStatus.Cancelled" /> and the workflow ending
///     <see cref="WorkflowOutcome.Cancelled" />.
/// </summary>
public sealed class WorkflowControlIntegrationTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PauseObservationWindow = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Runs a workflow with <see cref="Setting.PerformanceSetting.MaxConcurrentJobs" /> = 1 (so the second job
    ///     queues behind whichever job holds the single slot), waits until job 0 is demonstrably mid-run
    ///     (<see cref="JobPhase.FillingText" /> with <see cref="JobSnapshot.CurrentIndex" /> &gt;= 1), pauses the
    ///     workflow, and verifies that:
    ///     <list type="bullet">
    ///         <item>the row that was in flight finishes and the runner then parks at its next checkpoint;</item>
    ///         <item>no further job progress happens while paused (no new snapshot, and the sibling job's
    ///         persisted state does not change — a still-queued job stays <see cref="JobStatus.Pending" /> at
    ///         index 0);</item>
    ///         <item>after <see cref="WorkflowController.ResumeWorkflowAsync" />, every job completes;</item>
    ///         <item>the workflow's terminal outcome is <see cref="WorkflowOutcome.Completed" />.</item>
    ///     </list>
    /// </summary>
    [Fact(DisplayName = "INTEGRATION: (b) pause stops progress at the next checkpoint, resume completes")]
    public async Task Pause_HaltsAtNextCheckpoint_ThenResumeCompletes()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        await using var harness = new WorkflowTestHarness();
        harness.Settings.MaxConcurrentJobs = 1;

        // Two jobs sharing MaxConcurrentJobs=1: one holds the single slot while the other waits for the gate.
        // The large row range keeps the running job busy long enough to observe it mid-run before pausing.
        var specs = new[]
        {
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18)),
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18))
        };
        var workflowId = await harness.StartAsync(specs, name: "pause-resume",
            TestContext.Current.CancellationToken);

        // Wait until job 0 has published at least one FillingText snapshot with CurrentIndex >= 1, i.e. it is
        // demonstrably inside the row loop rather than still being queued or creating output.
        await WaitForLiveEventAsync(harness, workflowId,
            j => j.JobId == 0 && j.Phase == JobPhase.FillingText && j.CurrentIndex >= 1,
            TestTimeout, TestContext.Current.CancellationToken);

        (await harness.Controller.PauseWorkflowAsync(workflowId)).Should().BeTrue();
        harness.Observer.Paused.Should().ContainSingle(p => p.Key == workflowId);

        // Settle: let the row that was in flight at pause time finish, so the runner parks at its next
        // checkpoint (pause is cooperative and applies between rows), then record the published count and
        // the persisted state of both jobs.
        await Task.Delay(PauseObservationWindow, TestContext.Current.CancellationToken);

        var countAtPause = harness.EventBus.Jobs.Count(j => j.WorkflowId == workflowId);

        await harness.Jobs.FlushAsync(TestContext.Current.CancellationToken);
        var jobsAtPause = (await harness.Jobs.GetByWorkflowIdAsync(workflowId,
            TestContext.Current.CancellationToken)).ToDictionary(j => j.JobId);

        // The running job (job 0, observed mid-row above) has not finished: it is parked between rows.
        jobsAtPause[0].Phase.Should().NotBe(JobPhase.Done);
        jobsAtPause[0].JobStatus.Should().NotBe(JobStatus.Complete);

        // Observe a second quiet window: while paused nothing may advance. The gate does not guarantee which
        // of the two jobs wins the single slot, so job 1 may already have completed before job 0 got it; if it
        // is still queued it must stay Pending at index 0, and either way its state must not change.
        await Task.Delay(PauseObservationWindow, TestContext.Current.CancellationToken);

        harness.EventBus.Jobs.Count(j => j.WorkflowId == workflowId).Should().Be(countAtPause,
            "no job published progress while paused");

        await harness.Jobs.FlushAsync(TestContext.Current.CancellationToken);
        var jobsAfterPause = (await harness.Jobs.GetByWorkflowIdAsync(workflowId,
            TestContext.Current.CancellationToken)).ToDictionary(j => j.JobId);
        jobsAfterPause[1].JobStatus.Should().Be(jobsAtPause[1].JobStatus,
            "the gate-queued job did not change state while the workflow was paused");
        jobsAfterPause[1].CurrentIndex.Should().Be(jobsAtPause[1].CurrentIndex,
            "the gate-queued job did not start while the workflow was paused");
        jobsAfterPause[1].JobStatus.Should().NotBe(JobStatus.Running,
            "no job may be running while the workflow is paused");

        // Resume. Wait for the workflow to terminate.
        (await harness.Controller.ResumeWorkflowAsync(workflowId)).Should().BeTrue();
        var terminal = await harness.Observer.WaitTerminalAsync(workflowId)
            .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        terminal.Outcome.Should().Be(WorkflowOutcome.Completed);
        var finalJobs = await harness.Jobs.GetByWorkflowIdAsync(workflowId, TestContext.Current.CancellationToken);
        finalJobs.Should().OnlyContain(j => j.JobStatus == JobStatus.Complete && j.Phase == JobPhase.Done);
    }

    /// <summary>
    ///     Pauses the workflow and then immediately calls
    ///     <see cref="WorkflowController.StopWorkflowAsync" />. The stop must exit promptly: the workflow
    ///     ends <see cref="WorkflowOutcome.Cancelled" />, every job row ends <see cref="JobStatus.Cancelled" />
    ///     (the in-flight job at its next checkpoint, the queued job without ever starting its first row),
    ///     and the engine's terminal waiter is signalled well under the test timeout.
    /// </summary>
    [Fact(DisplayName = "INTEGRATION: (b) stop while paused exits promptly with cancelled job rows")]
    public async Task StopWhilePaused_TerminatesWithCancelledJobRowsPromptly()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        await using var harness = new WorkflowTestHarness();
        harness.Settings.MaxConcurrentJobs = 1;

        // The big row range keeps job 0 from finishing before the pause even on a fast machine.
        var specs = new[]
        {
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18)),
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18))
        };
        var workflowId = await harness.StartAsync(specs, name: "stop-while-paused",
            TestContext.Current.CancellationToken);

        // Wait for the first job to be running.
        await harness.WaitForJobsAsync(workflowId,
            jobs => jobs.Count == 2 && jobs.Any(j => j.JobStatus == JobStatus.Running),
            TestTimeout, TestContext.Current.CancellationToken);

        (await harness.Controller.PauseWorkflowAsync(workflowId)).Should().BeTrue();

        // Brief settle so the pause signal fully propagates to all runner contexts.
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Stop while paused.
        var stopReturned = await harness.Controller.StopWorkflowAsync(workflowId);
        stopReturned.Should().BeTrue();

        // The engine's terminal waiter must complete within the tight StopTimeout — this verifies the
        // engine unblocks the pause gate when stop is requested (otherwise the workflow would hang
        // forever on its next CheckpointAsync).
        var terminal = await harness.Observer.WaitTerminalAsync(workflowId)
            .WaitAsync(StopTimeout, TestContext.Current.CancellationToken);

        terminal.Outcome.Should().Be(WorkflowOutcome.Cancelled);

        // Every job row that was not already terminal at the moment of stop must end Cancelled.
        var finalJobs = await harness.Jobs.GetByWorkflowIdAsync(workflowId, TestContext.Current.CancellationToken);
        finalJobs.Should().HaveCount(2);
        finalJobs.Should().OnlyContain(j => j.JobStatus == JobStatus.Cancelled,
            "AGENTS.md: 'cancelling the workflow marks every job that has not finished Cancelled'");
    }

    /// <summary>
    ///     Stops the workflow with <c>waitForExit: true</c>: the call may only return once the runner has unwound
    ///     and its terminal outcome was forwarded, so the workflow and every job row are <em>immediately</em>
    ///     readable as <see cref="JobStatus.Cancelled" /> with no waiting on the observer and no later re-write.
    ///     A second stop then reports the workflow is no longer registered.
    /// </summary>
    [Fact(DisplayName = "INTEGRATION: (b) stop with wait-for-exit commits cancelled rows before returning")]
    public async Task StopWaitForExit_ReturnsOnlyAfterWorkflowAndJobRowsAreCancelled()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        await using var harness = new WorkflowTestHarness();
        harness.Settings.MaxConcurrentJobs = 1;

        var specs = new[]
        {
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18)),
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18))
        };
        var workflowId = await harness.StartAsync(specs, name: "stop-wait-exit",
            TestContext.Current.CancellationToken);

        await harness.WaitForJobsAsync(workflowId,
            jobs => jobs.Count == 2 && jobs.Any(j => j.JobStatus == JobStatus.Running),
            TestTimeout, TestContext.Current.CancellationToken);

        (await harness.Controller.StopWorkflowAsync(workflowId, waitForExit: true)).Should().BeTrue();

        // Deliberately no harness.Observer.WaitTerminalAsync here: waitForExit promises the terminal callback has
        // already been forwarded, so the persisted rows must be readable right away.
        var workflow = await harness.Workflows.GetAsync(workflowId, TestContext.Current.CancellationToken);
        workflow.Should().NotBeNull();
        workflow!.Status.Should().Be(JobStatus.Cancelled);

        var jobs = await harness.Jobs.GetByWorkflowIdAsync(workflowId, TestContext.Current.CancellationToken);
        jobs.Should().HaveCount(2);
        jobs.Should().OnlyContain(j => j.JobStatus == JobStatus.Cancelled,
            "the workflow had fully exited before StopWorkflowAsync returned");

        (await harness.Controller.StopWorkflowAsync(workflowId)).Should().BeFalse(
            "the workflow is no longer registered once it has exited");
    }

    /// <summary>
    ///     Polls the harness's event bus (20 ms delay between passes) until an already-published snapshot of
    ///     <paramref name="workflowId" /> matches <paramref name="predicate" />, or the timeout elapses. The bus
    ///     is live (every <see cref="JobExecutor" /> report passes through it), so this is a simple polling
    ///     sync point on the runner's progress.
    /// </summary>
    private static async Task WaitForLiveEventAsync(
        WorkflowTestHarness harness,
        string workflowId,
        Func<JobSnapshot, bool> predicate,
        TimeSpan timeout,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            foreach (var j in harness.EventBus.Jobs)
            {
                if (j.WorkflowId != workflowId) continue;
                if (predicate(j)) return;
            }

            await Task.Delay(20, ct);
        }

        throw new TimeoutException(
            $"Predicate did not become true within {timeout}. Last snapshot for {workflowId}: " +
            string.Join(", ", harness.EventBus.Jobs
                .Where(j => j.WorkflowId == workflowId)
                .Select(j => $"[{j.JobId} {j.JobStatus} {j.Phase} idx={j.CurrentIndex}]")));
    }
}
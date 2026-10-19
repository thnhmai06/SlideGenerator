/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: JobOutputIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text;
using FluentAssertions;
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Workflows.Engine;
using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     End-to-end checks that a job writes into its working (<c>.partial</c>) file and only replaces the final
///     result once every row has been generated: an existing result must survive a cancelled run, and a
///     completed run must swap the new presentation in atomically and leave no working file behind.
/// </summary>
[Trait("Category", "Integration")]
public sealed class JobOutputIntegrationTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

    // Mirrors JobExecutor.SaveEveryRows: the first periodic save of the working file happens after this many rows.
    private const int FirstSaveRow = 10;

    private static readonly byte[] Sentinel = Encoding.UTF8.GetBytes("previous result that must survive");

    /// <summary>
    ///     Runs a 1-job workflow over an existing result: the previous bytes must be replaced by the freshly
    ///     generated presentation, and the working file must be gone once the job completes.
    /// </summary>
    [Fact]
    public async Task Job_Completes_ReplacesAnExistingResultAndLeavesNoWorkingFile()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        var ct = TestContext.Current.CancellationToken;
        await using var harness = new WorkflowTestHarness();
        var outputPath = Path.Combine(harness.TempRoot, "output.pptx");
        await File.WriteAllBytesAsync(outputPath, Sentinel, ct);

        var spec = harness.BuildSpec(rowRange: new IndexRangeFilter(1, 3), outputPath: outputPath);
        var workflowId = await harness.StartAsync([spec], name: "output-commit", ct);

        var terminal = await harness.Observer.WaitTerminalAsync(workflowId).WaitAsync(TestTimeout, ct);
        terminal.Outcome.Should().Be(WorkflowOutcome.Completed);

        var bytes = await File.ReadAllBytesAsync(outputPath, ct);
        bytes.Length.Should().BeGreaterThan(Sentinel.Length,
            "the freshly generated presentation replaced the sentinel result");
        bytes[0].Should().Be((byte)'P', "a valid .pptx is a zip archive");
        bytes[1].Should().Be((byte)'K', "a valid .pptx is a zip archive");
        File.Exists(JobOutput.GetPartialPath(outputPath)).Should().BeFalse(
            "a completed job commits its working file onto the final output path");
    }

    /// <summary>
    ///     Stops a 1-job workflow mid-run: the previous result must stay untouched for the whole run and after
    ///     the stop, the working file must be discarded, and the job row must end Cancelled.
    /// </summary>
    [Fact]
    public async Task Job_IsStoppedMidway_KeepsThePreviousResultAndRemovesTheWorkingFile()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        var ct = TestContext.Current.CancellationToken;
        await using var harness = new WorkflowTestHarness();
        var outputPath = Path.Combine(harness.TempRoot, "output.pptx");
        await File.WriteAllBytesAsync(outputPath, Sentinel, ct);
        var partialPath = JobOutput.GetPartialPath(outputPath);

        var spec = harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18), outputPath: outputPath);
        var workflowId = await harness.StartAsync([spec], name: "output-cancel", ct);

        await WaitForLiveEventAsync(harness, workflowId,
            j => j.JobId == 0 && j.JobStatus == JobStatus.Running && j.Phase == JobPhase.Generating &&
                 j.CurrentIndex >= 1,
            TestTimeout, ct);

        File.Exists(partialPath).Should().BeTrue("a running job writes into its working file");
        (await File.ReadAllBytesAsync(outputPath, ct)).Should().Equal(Sentinel,
            "the previous result stays untouched while the new one is still being written");

        (await harness.Controller.StopWorkflowAsync(workflowId, waitForExit: true)).Should().BeTrue();

        (await File.ReadAllBytesAsync(outputPath, ct)).Should().Equal(Sentinel,
            "a cancelled job never replaces the previous result");
        File.Exists(partialPath).Should().BeFalse("a cancelled job discards its working file");

        var jobs = await harness.Jobs.GetByWorkflowIdAsync(workflowId, ct);
        jobs.Should().ContainSingle().Which.JobStatus.Should().Be(JobStatus.Cancelled);
    }

    /// <summary>
    ///     Runs a 1-job workflow over 18 rows, waits until the live row reports reach the first periodic save
    ///     boundary, pauses the job, then reads the durably persisted index and asserts that the working file
    ///     holds at least as many slides as that index claims. This is the "the DB is never ahead of the file"
    ///     invariant.
    /// </summary>
    [Fact]
    public async Task Job_PersistedIndexNeverExceedsTheRowsSavedInTheWorkingFile()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        var ct = TestContext.Current.CancellationToken;
        await using var harness = new WorkflowTestHarness();

        var spec = harness.BuildSpec(rowRange: new IndexRangeFilter(1, 18));
        var outputPath = spec.OutputPath;
        var partialPath = JobOutput.GetPartialPath(outputPath);
        var workflowId = await harness.StartAsync([spec], name: "output-invariant", ct);

        // Wait for the live row report at/past the first save boundary (row 10; see JobExecutor's SaveEveryRows)
        // before pausing: the row in flight then finishes its save and the runner parks before the last row
        // commits, so the working file is guaranteed to exist and to hold the first saved batch.
        await WaitForLiveEventAsync(harness, workflowId,
            j => j.Phase == JobPhase.Generating && j.CurrentIndex >= FirstSaveRow,
            TestTimeout, ct);

        (await harness.Controller.PauseWorkflowAsync(workflowId)).Should().BeTrue();

        // The persisted index only advances on a save, so observing it at >= 1 in the DB proves one happened.
        await harness.WaitForJobsAsync(workflowId,
            jobs => jobs.Count == 1 && jobs[0].Phase == JobPhase.Generating && jobs[0].CurrentIndex >= 1,
            TestTimeout, ct);

        // Settle: the row in flight at pause time finishes and the runner parks at its next checkpoint.
        await Task.Delay(300, ct);

        await harness.Jobs.FlushAsync(ct);
        var persistedIndex = (await harness.Jobs.GetByWorkflowIdAsync(workflowId, ct)).Single().CurrentIndex;
        persistedIndex.Should().BeGreaterThanOrEqualTo(1);

        File.Exists(partialPath).Should().BeTrue("a paused job keeps its working file");

        // Inspect a copy: the paused job still holds the working file open.
        var copyPath = Path.Combine(harness.TempRoot, "partial-copy.pptx");
        File.Copy(partialPath, copyPath, overwrite: true);
        int slideCountInFile;
        using (var presentation = await harness.OpenPresentationReadOnlyAsync(copyPath, ct))
        {
            slideCountInFile = presentation.SlidesCount;
        }

        slideCountInFile.Should().BeGreaterThanOrEqualTo(persistedIndex,
            "the persisted index counts rows already saved in the working file, so the file can never lag behind it");

        (await harness.Controller.ResumeWorkflowAsync(workflowId)).Should().BeTrue();
        var terminal = await harness.Observer.WaitTerminalAsync(workflowId).WaitAsync(TestTimeout, ct);
        terminal.Outcome.Should().Be(WorkflowOutcome.Completed);
    }

    /// <summary>
    ///     Polls the harness's live event bus until an already-published snapshot of
    ///     <paramref name="workflowId" /> matches <paramref name="predicate" />, or the timeout elapses.
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

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: WorkflowResumeIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text;
using FluentAssertions;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Workflows;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Workflows.Engine;
using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     End-to-end Request -&gt; Workflow -&gt; Job scenario (d): crash-resume. A workflow is run part-way
///     through, then the engine is cancelled and the workflow row's status is rolled back to Running to
///     reproduce the DB state a real crash (process killed mid-row) would leave behind; a brand-new
///     engine over the same SQLite file picks the workflow up at startup via
///     <see cref="GeneratorResumeSource" />, continues from the persisted
///     (<see cref="JobPhase" />, <see cref="JobSnapshot.CurrentIndex" />), runs to
///     <see cref="JobStatus.Complete" />, and keeps appending to the same <c>.log</c> file path stored on
///     <see cref="WorkflowRecord.LogPath" />.
/// </summary>
public sealed class WorkflowResumeIntegrationTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     Runs a single job, waits for the executor to enter <see cref="JobPhase.FillingText" /> with
    ///     <see cref="JobSnapshot.CurrentIndex" /> &gt;= 1, then cancels the first engine and rewinds the
    ///     <c>Workflows</c> row's status back to <see cref="JobStatus.Running" /> (the crash the engine
    ///     itself prevented the terminal write from completing). A second harness over the same DB then
    ///     calls <see cref="IWorkflowController.InitializeAsync" />; the resumed
    ///     <see cref="SlideGenerator.Generator.Workflows.SlideGenerationWorkflow" /> loads the job from its
    ///     persisted <see cref="JobPhase" /> + <see cref="JobSnapshot.CurrentIndex" /> and finishes. The
    ///     resumed run appends to the SAME log file as the first run.
    /// </summary>
    [Fact(DisplayName =
        "INTEGRATION: (d) crash-resume continues from persisted Phase/CurrentIndex and reuses the log file")]
    public async Task CrashThenResume_PicksUpFromPersistedPhaseAndIndex_AndAppendsToSameLog()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        // First harness: a real engine + real workflow controller, against a fresh temp SQLite. Use a
        // single job and a small row range so the test completes quickly even when the resume re-runs
        // most of the rows. Everything below runs inside try/finally: `first` owns the temp directory and
        // the SQLite anchor, so it must outlive `second` (which shares its DB file) and be disposed last.
        var first = new WorkflowTestHarness();
        JobSnapshot midSnapshot;

        try
        {
            var specs = new[]
            {
                first.BuildSpec(rowRange: new IndexRangeFilter(1, 3))
            };
            var workflowId = await first.StartAsync(specs, name: "resume-test",
                TestContext.Current.CancellationToken);

            // Wait until the job reaches FillingText and CurrentIndex >= 1 (at least one row processed).
            await first.WaitForJobsAsync(workflowId,
                jobs => jobs.Count == 1 && jobs[0].Phase == JobPhase.FillingText && jobs[0].CurrentIndex >= 1,
                TestTimeout, TestContext.Current.CancellationToken);

            // Capture the persisted (Phase, CurrentIndex) — the resume must continue from exactly this.
            midSnapshot = (await first.Jobs.GetByWorkflowIdAsync(workflowId,
                TestContext.Current.CancellationToken))[0];
            midSnapshot.Phase.Should().Be(JobPhase.FillingText);
            midSnapshot.CurrentIndex.Should().BeGreaterThanOrEqualTo(1);

            // Capture the workflow row's LogPath so the resumed run can be compared to the same file.
            var workflowRow = await first.Workflows.GetAsync(workflowId, TestContext.Current.CancellationToken);
            var logPath = workflowRow!.LogPath;

            // Force a flush so the rows we've observed are durable on disk before we kill the engine.
            await first.Jobs.FlushAsync(TestContext.Current.CancellationToken);

            File.Exists(logPath).Should().BeTrue(
                "the LoggingWorkflowRunner opened the file during the first run");

            // Simulate the crash: cancel the engine's CTS (the same path StopAsync takes), wait for the
            // engine's terminal callback to durably land Cancelled in the buffer, then rewind the workflow
            // and job rows back to Running. A real kill -9 leaves the engine mid-write and the rows at
            // whatever was last persisted (before any terminal Cancelled write completed); the wrapper here
            // replicates that explicitly because the engine otherwise does write Cancelled synchronously
            // after StopAsync. The wait is the deterministic synchronization point: without it the
            // engine's background task could overwrite Running with Cancelled after our rewind flush.
            await first.Engine.StopAsync(workflowId);

            // Drain the engine's terminal write (OnTerminalAsync -> workflows repository, plus the
            // per-job PersistAndPublishAsync). Both routes land in the same buffered repo; the wait
            // ensures Cancelled is durably persisted before we rewind.
            await first.Observer.WaitTerminalAsync(workflowId)
                .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

            // Rewind the workflow row and every job row back to Running. FlushAsync now is the final
            // write the row will see before the new engine takes over.
            await first.Workflows.UpdateStatusAsync(workflowId, JobStatus.Running,
                TestContext.Current.CancellationToken);

            var midJobsForRewind = await first.Jobs.GetByWorkflowIdAsync(workflowId,
                TestContext.Current.CancellationToken);
            foreach (var j in midJobsForRewind)
            {
                first.Jobs.Enqueue(j with { JobStatus = JobStatus.Running });
            }

            await first.Jobs.FlushAsync(TestContext.Current.CancellationToken);

            // Cleanly shut the first engine down so its background task exits.
            await first.ShutdownAsync();

            // Capture the line count from the first run before resuming into the same file.
            var firstRunLineCount = CountLines(logPath);

            // Second harness: same SQLite file, fresh engine, fresh resume source.
            await using var second = WorkflowTestHarness.OpenResumedOn(first);

            // InitializeAsync drives the resume source; the new engine's Task.Run picks the workflow up.
            await second.InitializeAsync(TestContext.Current.CancellationToken);

            var terminal = await second.Observer.WaitTerminalAsync(workflowId)
                .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

            if (terminal.Outcome != WorkflowOutcome.Completed)
            {
                var logDump = string.Join("\n",
                    second.LogNotifier.Entries
                        .Where(e => e.Path.StartsWith(workflowId))
                        .Select(e => $"  [{e.Level}] {e.Path} :: {e.Info}"));
                var eventDump = string.Join("\n",
                    second.EventBus.Jobs
                        .Where(j => j.WorkflowId == workflowId)
                        .OrderBy(j => j.Timestamp)
                        .Select(j => $"  [{j.Timestamp:HH:mm:ss.fff}] {j.JobStatus} {j.Phase} idx={j.CurrentIndex}"));
                throw new InvalidOperationException(
                    $"Resumed workflow {workflowId} outcome={terminal.Outcome}. " +
                    $"Terminal exception: {terminal.Exception?.GetType().Name}: {terminal.Exception?.Message}\n" +
                    $"Stack: {terminal.Exception?.StackTrace}\nLog entries:\n{logDump}\nEvent timeline:\n{eventDump}");
            }

            terminal.Outcome.Should().Be(WorkflowOutcome.Completed,
                "the resumed workflow must run its remaining rows to completion");

            // The resumed run CONTINUED rather than restarting: no published snapshot may sit in a phase
            // earlier than the persisted one, and same-phase snapshots may never report an index before
            // the persisted one (the executor resumes the FillingText loop at CurrentIndex, so its first
            // report is CurrentIndex + 1 — re-reporting the same index is tolerated too).
            var resumedSnapshots = second.EventBus.Jobs
                .Where(j => j.WorkflowId == workflowId)
                .ToList();
            resumedSnapshots.Should().NotBeEmpty("the resumed run reports job progress");
            resumedSnapshots.Should().NotContain(j => j.Phase < midSnapshot.Phase,
                $"the resumed run continues from the persisted phase ({midSnapshot.Phase}) and never re-runs an earlier one");
            resumedSnapshots.Where(j => j.Phase == midSnapshot.Phase)
                .Should().OnlyContain(j => j.CurrentIndex >= midSnapshot.CurrentIndex,
                    $"the resumed {midSnapshot.Phase} loop starts at the persisted index ({midSnapshot.CurrentIndex})");

            // Persisted job state reflects the post-resume run (terminal = Complete, Phase = Done).
            var finalJobs = await second.Jobs.GetByWorkflowIdAsync(workflowId, TestContext.Current.CancellationToken);
            finalJobs.Should().ContainSingle();
            finalJobs[0].JobStatus.Should().Be(JobStatus.Complete);
            finalJobs[0].Phase.Should().Be(JobPhase.Done);
            finalJobs[0].CurrentIndex.Should().Be(3,
                "IndexRangeFilter(1, 3) yields 3 rows; the resumed run reaches the last one");

            // The workflow row itself is now Complete.
            var workflowRowAfter = await second.Workflows.GetAsync(workflowId, TestContext.Current.CancellationToken);
            workflowRowAfter!.Status.Should().Be(JobStatus.Complete);

            // The resumed run appended to the same log file (the line count went up — no fresh file was
            // created, no other file is being written).
            File.Exists(logPath).Should().BeTrue();
            var finalLineCount = CountLines(logPath);
            finalLineCount.Should().BeGreaterThan(firstRunLineCount,
                "the resumed run must append log lines to the same .log path the first run was writing to");
        }
        finally
        {
            // `second` is already disposed (await using, at the end of the try block); `first` owns the
            // temp directory and the SQLite file, so it is disposed last.
            await first.DisposeAsync();
        }
    }

    /// <summary>Counts newline-terminated lines in <paramref name="path" /> — used to assert the log was appended to.</summary>
    private static long CountLines(string path)
    {
        var count = 0L;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (reader.ReadLine() != null)
        {
            count++;
        }

        return count;
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: WorkflowFailureIntegrationTests.cs
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
using SlideGenerator.Recipe.Models;
using SlideGenerator.Workflows.Engine;
using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     End-to-end Request -&gt; Workflow -&gt; Job scenario (c): one job of the workflow fails (its
///     worksheet name does not exist, which <see cref="JobExecutor" /> rejects with an
///     <see cref="InvalidOperationException" /> before any row is processed). The failing job ends
///     <see cref="JobStatus.Error" />; its sibling jobs end <see cref="JobStatus.Complete" />; the workflow
///     row ends <see cref="JobStatus.Complete" /> (AGENTS.md: a failing job does not stop them).
/// </summary>
public sealed class WorkflowFailureIntegrationTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     Runs a workflow with 3 jobs: the middle one has a worksheet name that does not exist in
    ///     <c>Data.xlsx</c>, so <see cref="JobExecutor" /> throws before any row is processed. Asserts
    ///     that the failing job ends <see cref="JobStatus.Error" />, the siblings end
    ///     <see cref="JobStatus.Complete" />, and the workflow row ends <see cref="JobStatus.Complete" />.
    /// </summary>
    [Fact(DisplayName = "INTEGRATION: (c) one failing job, siblings complete, workflow completes")]
    public async Task OneFailingJob_SiblingsComplete_WorkflowCompletes()
    {
        if (!WorkflowTestHarness.HasLicense())
        {
            Assert.Skip("SYNCFUSION_LICENSE_KEY not set — add the secret to run this integration test");
        }

        await using var harness = new WorkflowTestHarness();
        harness.Settings.MaxConcurrentJobs = 3;

        var specs = new[]
        {
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 3)),
            harness.BuildSpec(worksheetName: "NonExistent", rowRange: new IndexRangeFilter(1, 3)),
            harness.BuildSpec(rowRange: new IndexRangeFilter(1, 3))
        };
        var workflowId = await harness.StartAsync(specs, name: "one-fails", TestContext.Current.CancellationToken);

        var terminal = await harness.Observer.WaitTerminalAsync(workflowId)
            .WaitAsync(TestTimeout, TestContext.Current.CancellationToken);

        terminal.Outcome.Should().Be(WorkflowOutcome.Completed,
            "SlideGenerationWorkflow swallows per-job exceptions and lets the workflow RunAsync return normally");

        var finalJobs = await harness.Jobs.GetByWorkflowIdAsync(workflowId, TestContext.Current.CancellationToken);
        finalJobs.Should().HaveCount(3);

        // Job 0 — sibling, must Complete.
        finalJobs.Single(j => j.JobId == 0).JobStatus.Should().Be(JobStatus.Complete);
        finalJobs.Single(j => j.JobId == 0).Phase.Should().Be(JobPhase.Done);

        // Job 1 — failing, must Error. Phase stays at CreatingOutput because the workbook-opener
        // (or worksheet lookup) threw before any phase advance was persisted.
        finalJobs.Single(j => j.JobId == 1).JobStatus.Should().Be(JobStatus.Error);

        // Job 2 — sibling, must Complete.
        finalJobs.Single(j => j.JobId == 2).JobStatus.Should().Be(JobStatus.Complete);
        finalJobs.Single(j => j.JobId == 2).Phase.Should().Be(JobPhase.Done);

        // The workflow row itself must reflect Complete (per AGENTS.md: any non-Pending/Running jobs roll
        // up to Complete; the engine never marks a workflow Faulted because of per-job errors).
        var workflowRow = await harness.Workflows.GetAsync(workflowId, TestContext.Current.CancellationToken);
        workflowRow.Should().NotBeNull();
        workflowRow.Status.Should().Be(JobStatus.Complete);
    }
}
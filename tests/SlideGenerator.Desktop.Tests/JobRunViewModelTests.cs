/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: JobRunViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Features.Runs.ViewModels;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="JobRunViewModel" />'s <c>TotalRows</c> plumbing (plan §3.5/§5.4 backend
///     contract) — construction from an initial <see cref="JobSummary" />, live patching from a
///     <see cref="JobSnapshot" />, and the live activity line from <see cref="RowProgress" />.
/// </summary>
public sealed class JobRunViewModelTests
{
    private static JobSpecification MakeSpec(string outputPath = "out.pptx")
    {
        return new JobSpecification("wb.xlsx", "Sheet1", null, null, "template.pptx", 1, [], [], outputPath);
    }

    [Fact]
    public void Constructor_SummaryHasTotalRows_CopiesTotalRows()
    {
        var summary = new JobSummary
        {
            JobStatus = JobStatus.Running,
            Phase = JobPhase.FillingText,
            CurrentIndex = 5,
            TotalRows = 42,
            OutputPath = "out.pptx",
            Logs = []
        };

        var vm = new JobRunViewModel(0, summary);

        vm.TotalRows.Should().Be(42);
    }

    [Fact]
    public void Constructor_SummaryHasNoTotalRows_TotalRowsIsNull()
    {
        var summary = new JobSummary
        {
            JobStatus = JobStatus.Pending,
            Phase = JobPhase.CreatingOutput,
            CurrentIndex = 0,
            TotalRows = null,
            OutputPath = "out.pptx",
            Logs = []
        };

        var vm = new JobRunViewModel(0, summary);

        vm.TotalRows.Should().BeNull();
    }

    [Fact]
    public void ApplyLiveUpdate_SnapshotHasTotalRows_UpdatesTotalRows()
    {
        var summary = new JobSummary
        {
            JobStatus = JobStatus.Running, Phase = JobPhase.FillingText, CurrentIndex = 1,
            TotalRows = null, OutputPath = "out.pptx", Logs = []
        };
        var vm = new JobRunViewModel(0, summary);
        var snapshot = new JobSnapshot("req-1", 0, JobStatus.Running, JobPhase.FillingImages, 7,
            MakeSpec(), DateTimeOffset.UtcNow, TotalRows: 100);

        vm.ApplyLiveUpdate(snapshot);

        vm.TotalRows.Should().Be(100);
        vm.CurrentIndex.Should().Be(7);
        vm.Phase.Should().Be(JobPhase.FillingImages);
    }

    [Fact]
    public void ApplyLiveUpdate_TerminalStatus_SetsCompletedAt()
    {
        var summary = new JobSummary
        {
            JobStatus = JobStatus.Running, Phase = JobPhase.FillingImages, CurrentIndex = 5,
            TotalRows = 10, OutputPath = "out.pptx", Logs = []
        };
        var vm = new JobRunViewModel(0, summary);
        var completedAt = DateTimeOffset.UtcNow;
        var snapshot = new JobSnapshot("req-1", 0, JobStatus.Complete, JobPhase.Done, 10,
            MakeSpec(), completedAt, TotalRows: 10);

        vm.ApplyLiveUpdate(snapshot);

        vm.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void ApplyLiveRowUpdate_OverwritesPreviousActivity_DoesNotAccumulate()
    {
        var summary = new JobSummary
        {
            JobStatus = JobStatus.Running, Phase = JobPhase.FillingImages, CurrentIndex = 0,
            TotalRows = 10, OutputPath = "out.pptx", Logs = []
        };
        var vm = new JobRunViewModel(0, summary);

        vm.ApplyLiveRowUpdate(new RowProgress
        {
            RequestId = "req-1", JobId = 0, RowIndex = 1, Status = RowStatus.Processing,
            Stage = RowStage.Downloading, Note = "downloading a.png", Timestamp = DateTimeOffset.UtcNow
        });
        vm.ApplyLiveRowUpdate(new RowProgress
        {
            RequestId = "req-1", JobId = 0, RowIndex = 2, Status = RowStatus.Processing,
            Stage = RowStage.CroppingImage, Note = "cropping b.png", Timestamp = DateTimeOffset.UtcNow
        });

        vm.CurrentActivityNote.Should().Be("cropping b.png");
    }
}

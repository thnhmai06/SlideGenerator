/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: WorkflowRunViewModelTests.cs
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
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Progress;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="WorkflowRunViewModel" />'s <c>TotalRows</c> plumbing (plan §3.5/§5.4 backend
///     contract) — construction from an initial <see cref="WorkflowSummary" />, live patching from a
///     <see cref="WorkflowSnapshot" />, and the live activity line from <see cref="RowProgress" />.
/// </summary>
public sealed class WorkflowRunViewModelTests
{
    private static WorkflowSpecification MakeSpec(string outputPath = "out.pptx")
    {
        return new WorkflowSpecification("wb.xlsx", "Sheet1", null, null, "template.pptx", 1, [], [], outputPath);
    }

    [Fact]
    public void Constructor_SummaryHasTotalRows_CopiesTotalRows()
    {
        var summary = new WorkflowSummary
        {
            WorkflowStatus = WorkflowStatus.Running,
            Phase = WorkflowPhase.FillingText,
            CurrentIndex = 5,
            TotalRows = 42,
            OutputPath = "out.pptx",
            Logs = []
        };

        var vm = new WorkflowRunViewModel(0, summary);

        vm.TotalRows.Should().Be(42);
    }

    [Fact]
    public void Constructor_SummaryHasNoTotalRows_TotalRowsIsNull()
    {
        var summary = new WorkflowSummary
        {
            WorkflowStatus = WorkflowStatus.Pending,
            Phase = WorkflowPhase.CreatingOutput,
            CurrentIndex = 0,
            TotalRows = null,
            OutputPath = "out.pptx",
            Logs = []
        };

        var vm = new WorkflowRunViewModel(0, summary);

        vm.TotalRows.Should().BeNull();
    }

    [Fact]
    public void ApplyLiveUpdate_SnapshotHasTotalRows_UpdatesTotalRows()
    {
        var summary = new WorkflowSummary
        {
            WorkflowStatus = WorkflowStatus.Running, Phase = WorkflowPhase.FillingText, CurrentIndex = 1,
            TotalRows = null, OutputPath = "out.pptx", Logs = []
        };
        var vm = new WorkflowRunViewModel(0, summary);
        var snapshot = new WorkflowSnapshot("req-1", 0, WorkflowStatus.Running, WorkflowPhase.FillingImages, 7,
            MakeSpec(), DateTimeOffset.UtcNow, TotalRows: 100);

        vm.ApplyLiveUpdate(snapshot);

        vm.TotalRows.Should().Be(100);
        vm.CurrentIndex.Should().Be(7);
        vm.Phase.Should().Be(WorkflowPhase.FillingImages);
    }

    [Fact]
    public void ApplyLiveUpdate_TerminalStatus_SetsCompletedAt()
    {
        var summary = new WorkflowSummary
        {
            WorkflowStatus = WorkflowStatus.Running, Phase = WorkflowPhase.FillingImages, CurrentIndex = 5,
            TotalRows = 10, OutputPath = "out.pptx", Logs = []
        };
        var vm = new WorkflowRunViewModel(0, summary);
        var completedAt = DateTimeOffset.UtcNow;
        var snapshot = new WorkflowSnapshot("req-1", 0, WorkflowStatus.Complete, WorkflowPhase.Done, 10,
            MakeSpec(), completedAt, TotalRows: 10);

        vm.ApplyLiveUpdate(snapshot);

        vm.CompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void ApplyLiveRowUpdate_OverwritesPreviousActivity_DoesNotAccumulate()
    {
        var summary = new WorkflowSummary
        {
            WorkflowStatus = WorkflowStatus.Running, Phase = WorkflowPhase.FillingImages, CurrentIndex = 0,
            TotalRows = 10, OutputPath = "out.pptx", Logs = []
        };
        var vm = new WorkflowRunViewModel(0, summary);

        vm.ApplyLiveRowUpdate(new RowProgress
        {
            RequestId = "req-1", WorkflowId = 0, RowIndex = 1, Status = RowStatus.Processing,
            Stage = RowStage.Downloading, Note = "downloading a.png", Timestamp = DateTimeOffset.UtcNow
        });
        vm.ApplyLiveRowUpdate(new RowProgress
        {
            RequestId = "req-1", WorkflowId = 0, RowIndex = 2, Status = RowStatus.Processing,
            Stage = RowStage.CroppingImage, Note = "cropping b.png", Timestamp = DateTimeOffset.UtcNow
        });

        vm.CurrentActivityNote.Should().Be("cropping b.png");
    }
}

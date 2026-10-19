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
using NSubstitute;
using SlideGenerator.Desktop.Features.Runs.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="WorkflowRunViewModel" />'s request-level aggregate status — derivable from
///     live job/workflow snapshots and exposed to the Runs UI via <see cref="WorkflowRunViewModel.Status" /> —
///     plus the command availability it drives.
/// </summary>
public sealed class WorkflowRunViewModelTests
{
    private const string WorkflowId = "req-1";

    private static JobSpecification MakeSpec(string outputPath = "out.pptx")
    {
        return new JobSpecification("wb.xlsx", "Sheet1", null, null, "template.pptx", 1, [], [], outputPath);
    }

    private static Summary MakeSummary(RequestStatus status, params JobStatus[] jobStatuses)
    {
        var jobs = jobStatuses
            .Select((jobStatus, index) => (Id: index, Summary: new JobSummary
            {
                JobStatus = jobStatus,
                Phase = JobPhase.FillingText,
                CurrentIndex = 0,
                OutputPath = $"out{index}.pptx",
                Logs = []
            }))
            .ToDictionary(x => x.Id, x => x.Summary);

        return new Summary
        {
            Request = new Request(1, "Test request", PresentationType.Pptx, "out"),
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            Logs = [],
            Jobs = jobs
        };
    }

    private static JobSnapshot MakeSnapshot(int jobId, JobStatus status, DateTimeOffset timestamp)
    {
        return new JobSnapshot(WorkflowId, jobId, status, JobPhase.Done, 0, MakeSpec(), timestamp);
    }

    private static WorkflowRunViewModel CreateViewModel(RequestStatus status, params JobStatus[] jobStatuses)
    {
        return new WorkflowRunViewModel(WorkflowId, MakeSummary(status, jobStatuses),
            Substitute.For<IService>(), Substitute.For<IDialogService>());
    }

    /// <summary>Every job ending in <c>Error</c> derives <see cref="RequestStatus.Error" /> and stamps the completion time.</summary>
    [Fact]
    public void ApplyLiveJobUpdate_AllJobsError_DerivesErrorAndSetsCompletedAt()
    {
        var vm = CreateViewModel(RequestStatus.Running, JobStatus.Running);

        vm.ApplyLiveJobUpdate(MakeSnapshot(0, JobStatus.Error, DateTimeOffset.UtcNow));

        vm.Status.Should().Be(RequestStatus.Error);
        vm.CompletedAt.Should().NotBeNull();
    }

    /// <summary>A mix of finished and failed jobs derives <see cref="RequestStatus.CompletedWithError" />, not a plain success or failure.</summary>
    [Fact]
    public void ApplyLiveJobUpdate_OneCompleteOneError_DerivesCompletedWithError()
    {
        var vm = CreateViewModel(RequestStatus.Running, JobStatus.Running, JobStatus.Running);

        vm.ApplyLiveJobUpdate(MakeSnapshot(0, JobStatus.Complete, DateTimeOffset.UtcNow));
        vm.ApplyLiveJobUpdate(MakeSnapshot(1, JobStatus.Error, DateTimeOffset.UtcNow));

        vm.Status.Should().Be(RequestStatus.CompletedWithError);
    }

    /// <summary>A workflow-level pause turns a still-running request into <c>Paused</c>, and resuming turns it back to <c>Running</c>.</summary>
    [Fact]
    public void ApplyLiveWorkflowUpdate_PausedThenRunning_DerivesPausedThenRunning()
    {
        var vm = CreateViewModel(RequestStatus.Running, JobStatus.Running);

        vm.ApplyLiveWorkflowUpdate(new WorkflowSnapshot(WorkflowId, JobStatus.Paused, DateTimeOffset.UtcNow));
        vm.Status.Should().Be(RequestStatus.Paused);

        vm.ApplyLiveWorkflowUpdate(new WorkflowSnapshot(WorkflowId, JobStatus.Running, DateTimeOffset.UtcNow));
        vm.Status.Should().Be(RequestStatus.Running);
    }

    /// <summary>A running request can be paused or stopped, but not resumed.</summary>
    [Fact]
    public void Commands_StatusRunning_PauseAndStopExecutableResumeNot()
    {
        var vm = CreateViewModel(RequestStatus.Running, JobStatus.Running);

        vm.PauseCommand.CanExecute(null).Should().BeTrue();
        vm.StopCommand.CanExecute(null).Should().BeTrue();
        vm.ResumeCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>A paused request can be resumed or stopped, but not paused again.</summary>
    [Fact]
    public void Commands_StatusPaused_ResumeAndStopExecutablePauseNot()
    {
        var vm = CreateViewModel(RequestStatus.Paused, JobStatus.Paused);

        vm.ResumeCommand.CanExecute(null).Should().BeTrue();
        vm.StopCommand.CanExecute(null).Should().BeTrue();
        vm.PauseCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>A failed request offers no control command — nothing is left to pause, resume, or stop.</summary>
    [Fact]
    public void Commands_StatusError_NoCommandExecutable()
    {
        var vm = CreateViewModel(RequestStatus.Error, JobStatus.Error);

        vm.PauseCommand.CanExecute(null).Should().BeFalse();
        vm.ResumeCommand.CanExecute(null).Should().BeFalse();
        vm.StopCommand.CanExecute(null).Should().BeFalse();
    }
}

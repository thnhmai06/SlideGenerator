/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: RunsViewModelFilterTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.Runs.Models;
using SlideGenerator.Desktop.Features.Runs.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Generator.Workflows.Models;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="RunsViewModel" />'s chip filtering — in particular that
///     <see cref="RunStatusFilter.Failed" /> surfaces both a fully failed request and a partially failed one,
///     while <see cref="RunStatusFilter.Done" /> stays limited to cleanly completed requests.
/// </summary>
public sealed class RunsViewModelFilterTests
{
    private static Summary MakeSummary(string name, RequestStatus status)
    {
        return new Summary
        {
            Request = new Request(1, name, PresentationType.Pptx, "out"),
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            Logs = [],
            Jobs = new Dictionary<int, JobSummary>
            {
                [0] = new()
                {
                    JobStatus = JobStatus.Complete,
                    Phase = JobPhase.Done,
                    CurrentIndex = 0,
                    OutputPath = "out.pptx",
                    Logs = []
                }
            }
        };
    }

    private static (RunsViewModel ViewModel, IService Service) CreateViewModel()
    {
        var progressHub = Substitute.For<IProgressHub>();
        progressHub.Workflows.Returns(new ObservableCollection<WorkflowSnapshot>());
        progressHub.Jobs.Returns(new ObservableCollection<JobSnapshot>());
        progressHub.Rows.Returns(new ObservableCollection<RowProgress>());

        var service = Substitute.For<IService>();
        service.ListActiveAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Summary>
            {
                ["running"] = MakeSummary("running", RequestStatus.Running)
            });
        service.ListCompletedAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Summary>
            {
                ["complete"] = MakeSummary("complete", RequestStatus.Complete),
                ["error"] = MakeSummary("error", RequestStatus.Error),
                ["completedWithError"] = MakeSummary("completedWithError", RequestStatus.CompletedWithError),
                ["cancelled"] = MakeSummary("cancelled", RequestStatus.Cancelled)
            });

        return (new RunsViewModel(service, progressHub, Substitute.For<IDialogService>()), service);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5, ct);
        condition().Should().BeTrue("condition did not become true within the timeout");
    }

    /// <summary>The initial load exposes all five requests under <see cref="RunStatusFilter.All" />.</summary>
    [Fact]
    public async Task Load_FiveRequestsUnderAllFilter_ListsEveryRequest()
    {
        var (vm, _) = CreateViewModel();
        var ct = TestContext.Current.CancellationToken;
        await vm.LoadCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => !vm.IsLoading && vm.FilteredRequests.Count == 5, ct);

        vm.FilteredRequests.Should().HaveCount(5);
    }

    /// <summary>
    ///     Each chip filters on the aggregate request status: Done keeps only the clean completion, Failed keeps
    ///     exactly Error + CompletedWithError, and the rest match one-for-one.
    /// </summary>
    [Fact]
    public async Task Filter_EachChip_MatchesOnlyItsRequestStatuses()
    {
        var (vm, _) = CreateViewModel();
        var ct = TestContext.Current.CancellationToken;
        await vm.LoadCommand.ExecuteAsync(null);
        await WaitUntilAsync(() => !vm.IsLoading && vm.FilteredRequests.Count == 5, ct);

        vm.Filter = RunStatusFilter.All;
        vm.FilteredRequests.Should().HaveCount(5);

        vm.Filter = RunStatusFilter.Running;
        vm.FilteredRequests.Select(r => r.Name).Should().Equal("running");

        vm.Filter = RunStatusFilter.Done;
        vm.FilteredRequests.Select(r => r.Name).Should().Equal("complete");

        vm.Filter = RunStatusFilter.Failed;
        vm.FilteredRequests.Select(r => r.Name).Should().BeEquivalentTo("error", "completedWithError");

        vm.Filter = RunStatusFilter.Cancelled;
        vm.FilteredRequests.Select(r => r.Name).Should().Equal("cancelled");
    }
}

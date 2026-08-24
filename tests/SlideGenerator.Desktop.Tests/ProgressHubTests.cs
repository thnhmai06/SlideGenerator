/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: ProgressHubTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="ProgressHub" />'s coalescing: job snapshots and row progress keep only the
///     latest value per key (last write wins), while log lines are never dropped. <c>Drain()</c> is called
///     directly rather than waiting on the real 250ms timer tick.
/// </summary>
public sealed class ProgressHubTests
{
    private static JobSnapshot Job(string requestId, int jobId, int currentIndex)
    {
        return new JobSnapshot(requestId, jobId, JobStatus.Running, JobPhase.FillingText, currentIndex,
            new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"), DateTimeOffset.UtcNow);
    }

    private static RowProgress Row(string requestId, int jobId, int rowIndex, RowStatus status)
    {
        return new RowProgress
        {
            RequestId = requestId, JobId = jobId, RowIndex = rowIndex, Status = status,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Two snapshots for the same (RequestId, JobId) coalesce into one — the most recent value.</summary>
    [Fact]
    public void Drain_TwoJobSnapshotsSameKey_KeepsOnlyLatest()
    {
        var eventBus = new GeneratingEventBus();
        using var hub = new ProgressHub(eventBus, new LogNotifier());

        eventBus.Publish(Job("req", 0, 1));
        eventBus.Publish(Job("req", 0, 5));
        hub.Drain();

        hub.Jobs.Should().ContainSingle();
        hub.Jobs[0].CurrentIndex.Should().Be(5);
    }

    /// <summary>Snapshots for different jobs both survive a drain — coalescing is per-key, not global.</summary>
    [Fact]
    public void Drain_DifferentJobKeys_KeepsBoth()
    {
        var eventBus = new GeneratingEventBus();
        using var hub = new ProgressHub(eventBus, new LogNotifier());

        eventBus.Publish(Job("req", 0, 1));
        eventBus.Publish(Job("req", 1, 1));
        hub.Drain();

        hub.Jobs.Should().HaveCount(2);
    }

    /// <summary>Row progress coalesces by the finer-grained (RequestId, JobId, RowIndex) key.</summary>
    [Fact]
    public void Drain_TwoRowUpdatesSameRow_KeepsOnlyLatestStatus()
    {
        var eventBus = new GeneratingEventBus();
        using var hub = new ProgressHub(eventBus, new LogNotifier());

        eventBus.Publish(Row("req", 0, 3, RowStatus.Processing));
        eventBus.Publish(Row("req", 0, 3, RowStatus.Done));
        hub.Drain();

        hub.Rows.Should().ContainSingle();
        hub.Rows[0].Status.Should().Be(RowStatus.Done);
    }

    /// <summary>Log lines are append-only — every line survives, none coalesced, even for the same request.</summary>
    [Fact]
    public void Drain_MultipleLogLines_KeepsAllInOrder()
    {
        var logNotifier = new LogNotifier();
        using var hub = new ProgressHub(new GeneratingEventBus(), logNotifier);

        var first = new LogEntry { Timestamp = DateTimeOffset.UtcNow, Path = "req", Level = "INF", Info = "first" };
        var second = new LogEntry { Timestamp = DateTimeOffset.UtcNow, Path = "req", Level = "INF", Info = "second" };
        logNotifier.Publish(first);
        logNotifier.Publish(second);
        hub.Drain();

        hub.Logs.Should().Equal(first, second);
    }

    /// <summary>A drain with nothing published does not add empty/placeholder entries.</summary>
    [Fact]
    public void Drain_NothingPublished_CollectionsStayEmpty()
    {
        using var hub = new ProgressHub(new GeneratingEventBus(), new LogNotifier());

        hub.Drain();

        hub.Jobs.Should().BeEmpty();
        hub.Rows.Should().BeEmpty();
        hub.Logs.Should().BeEmpty();
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: WorkflowsRepositoryTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Workflows.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Database;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="WorkflowsRepository" />, verifying that a <see cref="WorkflowSnapshot" /> — including
///     its <see cref="RowFilter" /> variants and instruction lists — round-trips through SQLite via an
///     in-memory shared-cache database.
/// </summary>
public sealed class WorkflowsRepositoryTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly WorkflowsRepository _repo;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database. The anchor connection keeps the
    ///     in-memory database alive across all short-lived per-operation connections.
    /// </summary>
    public WorkflowsRepositoryTests()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"memory_{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };
        _anchor = new SqliteConnection(builder.ConnectionString);
        _anchor.Open();
        DatabaseMigrator.Migrate(builder.ConnectionString);
        _repo = new WorkflowsRepository(builder, NullLogger<WorkflowsRepository>.Instance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
    }

    private static WorkflowSpecification Spec(RowFilter? rowFilter = null)
    {
        return new WorkflowSpecification(
            "wb.xlsx", "Sheet1", null, rowFilter, "template.pptx", 1,
            [new TextInstruction(new HashSet<string> { "{{Name}}" }, [new ColumnIdentifier("Name")])],
            [], "out.pptx");
    }

    /// <summary>Verifies that a freshly enqueued and flushed workflow round-trips its resume state exactly.</summary>
    [Fact]
    public async Task EnqueueThenFlush_ThenGetByRequestId_RoundTripsWorkflowSnapshot()
    {
        var record = new WorkflowSnapshot("req1", 0, WorkflowStatus.Running, WorkflowPhase.FillingText, 3, Spec(),
            DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var workflows = await _repo.GetByRequestIdAsync("req1", TestContext.Current.CancellationToken);

        workflows.Should().ContainSingle();
        workflows[0].WorkflowStatus.Should().Be(WorkflowStatus.Running);
        workflows[0].Phase.Should().Be(WorkflowPhase.FillingText);
        workflows[0].CurrentIndex.Should().Be(3);
        workflows[0].Specification.TextInstructions.Should().ContainSingle();
    }

    /// <summary>Verifies that an <see cref="IndexRangeFilter" /> round-trips via its explicit start/end columns.</summary>
    [Fact]
    public async Task EnqueueWithIndexRangeFilter_RoundTripsFilterBounds()
    {
        var record = new WorkflowSnapshot("req2", 0, WorkflowStatus.Pending, WorkflowPhase.CreatingOutput, 0,
            Spec(new IndexRangeFilter(2, 5)), DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var workflows = await _repo.GetByRequestIdAsync("req2", TestContext.Current.CancellationToken);

        workflows[0].Specification.RowFilter.Should().BeOfType<IndexRangeFilter>()
            .Which.Should().BeEquivalentTo(new IndexRangeFilter(2, 5));
    }

    /// <summary>Verifies that a <see cref="PartitionBlockFilter" /> round-trips via its partition columns.</summary>
    [Fact]
    public async Task EnqueueWithPartitionBlockFilter_RoundTripsPartitionValues()
    {
        var record = new WorkflowSnapshot("req3", 1, WorkflowStatus.Paused, WorkflowPhase.FillingImages, 4,
            Spec(new PartitionBlockFilter(1, 3)), DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var workflows = await _repo.GetByRequestIdAsync("req3", TestContext.Current.CancellationToken);

        workflows[0].Specification.RowFilter.Should().BeOfType<PartitionBlockFilter>()
            .Which.Should().BeEquivalentTo(new PartitionBlockFilter(1, 3));
    }

    /// <summary>Verifies that <c>GetNonTerminalAsync</c> excludes Complete/Canceled workflows.</summary>
    [Fact]
    public async Task GetNonTerminalAsync_ExcludesCompleteAndCancelled()
    {
        _repo.Enqueue(new WorkflowSnapshot("req4", 0, WorkflowStatus.Complete, WorkflowPhase.Done, 5, Spec(), DateTimeOffset.UtcNow));
        _repo.Enqueue(new WorkflowSnapshot("req4", 1, WorkflowStatus.Running, WorkflowPhase.FillingText, 1, Spec(),
            DateTimeOffset.UtcNow));
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var nonTerminal = await _repo.GetNonTerminalAsync(TestContext.Current.CancellationToken);

        nonTerminal.Should().ContainSingle(j => j.WorkflowId == 1);
    }

    /// <summary>Verifies that deleting a request removes every workflow row belonging to it.</summary>
    [Fact]
    public async Task DeleteByRequestIdAsync_RemovesAllWorkflowsOfThatRequest()
    {
        _repo.Enqueue(new WorkflowSnapshot("req5", 0, WorkflowStatus.Complete, WorkflowPhase.Done, 1, Spec(), DateTimeOffset.UtcNow));
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        await _repo.DeleteByRequestIdAsync("req5", TestContext.Current.CancellationToken);
        var workflows = await _repo.GetByRequestIdAsync("req5", TestContext.Current.CancellationToken);

        workflows.Should().BeEmpty();
    }

    /// <summary>Verifies that <see cref="WorkflowSnapshot.TotalRows" /> round-trips through the
    ///     <c>002_add-total-rows-to-jobs.sql</c> migration's column (plan §3.5's <c>TotalRows</c> contract).</summary>
    [Fact]
    public async Task EnqueueWithTotalRows_RoundTripsTotalRows()
    {
        var record = new WorkflowSnapshot("req6", 0, WorkflowStatus.Running, WorkflowPhase.CreatingSlides, 2, Spec(),
            DateTimeOffset.UtcNow, 42);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var workflows = await _repo.GetByRequestIdAsync("req6", TestContext.Current.CancellationToken);

        workflows[0].TotalRows.Should().Be(42);
    }

    /// <summary>Verifies that a snapshot with no <see cref="WorkflowSnapshot.TotalRows" /> (the default) persists
    ///     and reads back as <see langword="null" /> rather than 0 or throwing.</summary>
    [Fact]
    public async Task EnqueueWithoutTotalRows_RoundTripsNull()
    {
        var record = new WorkflowSnapshot("req7", 0, WorkflowStatus.Pending, WorkflowPhase.CreatingOutput, 0, Spec(),
            DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var workflows = await _repo.GetByRequestIdAsync("req7", TestContext.Current.CancellationToken);

        workflows[0].TotalRows.Should().BeNull();
    }
}
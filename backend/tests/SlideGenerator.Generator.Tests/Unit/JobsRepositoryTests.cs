/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: JobsRepositoryTests.cs
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
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Database;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="JobsRepository" />, verifying that a <see cref="JobSnapshot" /> — including
///     its <see cref="RowFilter" /> variants and instruction lists — round-trips through SQLite via an
///     in-memory shared-cache database.
/// </summary>
public sealed class JobsRepositoryTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly JobsRepository _repo;

    public JobsRepositoryTests()
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
        _repo = new JobsRepository(builder, NullLogger<JobsRepository>.Instance);
    }

    public void Dispose() => _anchor.Dispose();

    private static JobSpecification Spec(RowFilter? rowFilter = null) => new(
        "wb.xlsx", "Sheet1", null, rowFilter, "template.pptx", 1,
        [new TextInstruction(new HashSet<string> { "{{Name}}" }, [new("Name")])],
        [], "out.pptx");

    /// <summary>Verifies that a freshly enqueued and flushed job round-trips its resume state exactly.</summary>
    [Fact]
    public async Task EnqueueThenFlush_ThenGetByRequestId_RoundTripsJobSnapshot()
    {
        var record = new JobSnapshot("req1", 0, JobStatus.Running, JobPhase.FillingText, 3, Spec(), DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var jobs = await _repo.GetByRequestIdAsync("req1", TestContext.Current.CancellationToken);

        jobs.Should().ContainSingle();
        jobs[0].JobStatus.Should().Be(JobStatus.Running);
        jobs[0].Phase.Should().Be(JobPhase.FillingText);
        jobs[0].CurrentIndex.Should().Be(3);
        jobs[0].Specification.TextInstructions.Should().ContainSingle();
    }

    /// <summary>Verifies that an <see cref="IndexRangeFilter" /> round-trips via its explicit start/end columns.</summary>
    [Fact]
    public async Task EnqueueWithIndexRangeFilter_RoundTripsFilterBounds()
    {
        var record = new JobSnapshot("req2", 0, JobStatus.Pending, JobPhase.CreatingOutput, 0,
            Spec(new IndexRangeFilter(2, 5)), DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var jobs = await _repo.GetByRequestIdAsync("req2", TestContext.Current.CancellationToken);

        jobs[0].Specification.RowFilter.Should().BeOfType<IndexRangeFilter>()
            .Which.Should().BeEquivalentTo(new IndexRangeFilter(2, 5));
    }

    /// <summary>Verifies that a <see cref="PartitionBlockFilter" /> round-trips via its partition columns.</summary>
    [Fact]
    public async Task EnqueueWithPartitionBlockFilter_RoundTripsPartitionValues()
    {
        var record = new JobSnapshot("req3", 1, JobStatus.Paused, JobPhase.FillingImages, 4,
            Spec(new PartitionBlockFilter(1, 3)), DateTimeOffset.UtcNow);
        _repo.Enqueue(record);
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var jobs = await _repo.GetByRequestIdAsync("req3", TestContext.Current.CancellationToken);

        jobs[0].Specification.RowFilter.Should().BeOfType<PartitionBlockFilter>()
            .Which.Should().BeEquivalentTo(new PartitionBlockFilter(1, 3));
    }

    /// <summary>Verifies that <c>GetNonTerminalAsync</c> excludes Complete/Cancelled jobs.</summary>
    [Fact]
    public async Task GetNonTerminalAsync_ExcludesCompleteAndCancelled()
    {
        _repo.Enqueue(new JobSnapshot("req4", 0, JobStatus.Complete, JobPhase.Done, 5, Spec(), DateTimeOffset.UtcNow));
        _repo.Enqueue(new JobSnapshot("req4", 1, JobStatus.Running, JobPhase.FillingText, 1, Spec(), DateTimeOffset.UtcNow));
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        var nonTerminal = await _repo.GetNonTerminalAsync(TestContext.Current.CancellationToken);

        nonTerminal.Should().ContainSingle(j => j.JobId == 1);
    }

    /// <summary>Verifies that deleting a request removes every job row belonging to it.</summary>
    [Fact]
    public async Task DeleteByRequestIdAsync_RemovesAllJobsOfThatRequest()
    {
        _repo.Enqueue(new JobSnapshot("req5", 0, JobStatus.Complete, JobPhase.Done, 1, Spec(), DateTimeOffset.UtcNow));
        await _repo.FlushAsync(TestContext.Current.CancellationToken);

        await _repo.DeleteByRequestIdAsync("req5", TestContext.Current.CancellationToken);
        var jobs = await _repo.GetByRequestIdAsync("req5", TestContext.Current.CancellationToken);

        jobs.Should().BeEmpty();
    }
}

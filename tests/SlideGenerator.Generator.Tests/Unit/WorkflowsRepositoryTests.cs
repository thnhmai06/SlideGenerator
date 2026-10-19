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
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Persistence;
using SlideGenerator.Settings.Database;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="WorkflowsRepository" />, verifying that a <see cref="WorkflowRecord" />
///     (the originating <see cref="Request" />, log path and lifecycle status) round-trips through SQLite via an
///     in-memory shared-cache database migrated with the real schema.
/// </summary>
public sealed class WorkflowsRepositoryTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly WorkflowsRepository _repo;

    /// <summary>Sets up a shared-cache in-memory database kept alive by an anchor connection.</summary>
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
        _repo = new WorkflowsRepository(builder);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
    }

    private static WorkflowRecord Record(string id, JobStatus status = JobStatus.Pending)
    {
        return new WorkflowRecord(id,
            new Request(7, "Report", PresentationType.Pptx, Path.GetTempPath(), true),
            "report.log", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), status);
    }

    /// <summary>A created workflow reads back with its request, log path, timestamp and status intact.</summary>
    [Fact]
    public async Task CreateThenGet_RoundTripsRecord()
    {
        var record = Record("wf-1");
        await _repo.CreateAsync(record, TestContext.Current.CancellationToken);

        var loaded = await _repo.GetAsync("wf-1", TestContext.Current.CancellationToken);

        loaded.Should().NotBeNull();
        loaded.Should().BeEquivalentTo(record);
    }

    /// <summary>Updating a workflow's status changes only the stored status.</summary>
    [Fact]
    public async Task UpdateStatus_PersistsNewStatus()
    {
        await _repo.CreateAsync(Record("wf-2"), TestContext.Current.CancellationToken);

        await _repo.UpdateStatusAsync("wf-2", JobStatus.Paused, TestContext.Current.CancellationToken);

        var loaded = await _repo.GetAsync("wf-2", TestContext.Current.CancellationToken);
        loaded!.Status.Should().Be(JobStatus.Paused);
        loaded.Request.Name.Should().Be("Report");
    }

    /// <summary>Only pending, running and paused workflows are returned for crash-resume.</summary>
    [Fact]
    public async Task GetNonTerminal_ReturnsOnlyUnfinishedWorkflows()
    {
        await _repo.CreateAsync(Record("pending", JobStatus.Pending), TestContext.Current.CancellationToken);
        await _repo.CreateAsync(Record("running", JobStatus.Running), TestContext.Current.CancellationToken);
        await _repo.CreateAsync(Record("paused", JobStatus.Paused), TestContext.Current.CancellationToken);
        await _repo.CreateAsync(Record("done", JobStatus.Complete), TestContext.Current.CancellationToken);
        await _repo.CreateAsync(Record("cancelled", JobStatus.Cancelled), TestContext.Current.CancellationToken);
        await _repo.CreateAsync(Record("failed", JobStatus.Error), TestContext.Current.CancellationToken);

        var unfinished = await _repo.GetNonTerminalAsync(TestContext.Current.CancellationToken);

        unfinished.Select(r => r.WorkflowId).Should().BeEquivalentTo("pending", "running", "paused");
    }

    /// <summary>Deleting removes the row, and getting a missing workflow returns null.</summary>
    [Fact]
    public async Task Delete_RemovesRow()
    {
        await _repo.CreateAsync(Record("wf-3"), TestContext.Current.CancellationToken);

        await _repo.DeleteAsync("wf-3", TestContext.Current.CancellationToken);

        (await _repo.GetAsync("wf-3", TestContext.Current.CancellationToken)).Should().BeNull();
    }
}

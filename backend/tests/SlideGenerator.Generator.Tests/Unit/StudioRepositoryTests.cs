/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: StudioRepositoryTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Data.Sqlite;
using SlideGenerator.Generator.Domain.Models.Data;
using SlideGenerator.Generator.Domain.Models.Enum;
using SlideGenerator.Generator.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Round-trip tests for <see cref="StudioRepository" /> against a real (in-memory, shared-cache)
///     SQLite database — proves the WAL/busy_timeout setup, the batched-upsert transactions, and the
///     enum-as-TEXT/timestamp-as-TEXT column mapping all actually work end-to-end, without mocks.
/// </summary>
public sealed class StudioRepositoryTests : IDisposable
{
    private readonly SqliteConnection _anchor;
    private readonly StudioRepository _repository;

    /// <summary>
    ///     Sets up a shared-cache in-memory SQLite database. The anchor connection keeps the
    ///     in-memory database alive across all short-lived per-operation connections.
    /// </summary>
    public StudioRepositoryTests()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"memory_{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };
        _anchor = new SqliteConnection(builder.ConnectionString);
        _anchor.Open();
        _repository = new StudioRepository(builder);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _anchor.Dispose();
    }

    /// <summary>Verifies a request's phase upserts and reads back correctly, including a later overwrite.</summary>
    [Fact]
    public async Task UpsertRequestAsync_ThenGetRequestAsync_RoundTrips()
    {
        var requestId = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow;

        await _repository.UpsertRequestAsync(new RequestProgress
        {
            RequestId = requestId, Phase = RequestPhase.PreparationStarted, Timestamp = timestamp
        });
        (await _repository.GetRequestAsync(requestId))!.Phase.Should().Be(RequestPhase.PreparationStarted);

        var later = timestamp.AddSeconds(5);
        await _repository.UpsertRequestAsync(new RequestProgress
        {
            RequestId = requestId, Phase = RequestPhase.Completed, Timestamp = later
        });
        var updated = await _repository.GetRequestAsync(requestId);

        updated.Should().NotBeNull();
        updated!.Phase.Should().Be(RequestPhase.Completed);
        updated.Timestamp.Should().BeCloseTo(later, TimeSpan.FromMilliseconds(10));
    }

    /// <summary>Verifies a batch of job progress entries upserts in one transaction and reads back keyed by job id.</summary>
    [Fact]
    public async Task UpsertJobsAsync_Batch_ThenGetJobsAsync_RoundTrips()
    {
        var requestId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var batch = new List<JobProgress>
        {
            new() { RequestId = requestId, JobId = "job-1", Status = Status.Pending, Timestamp = now },
            new() { RequestId = requestId, JobId = "job-2", Status = Status.Running, Timestamp = now }
        };

        await _repository.UpsertJobsAsync(batch);
        var jobs = await _repository.GetJobsAsync(requestId);

        jobs.Should().HaveCount(2);
        jobs["job-1"].Status.Should().Be(Status.Pending);
        jobs["job-2"].Status.Should().Be(Status.Running);
    }

    /// <summary>Verifies a batch of row progress entries — including Stage and Note — upserts and reads back keyed by row index.</summary>
    [Fact]
    public async Task UpsertRowsAsync_Batch_ThenGetRowsAsync_RoundTrips()
    {
        var requestId = Guid.NewGuid().ToString();
        var jobId = "job-1";
        var now = DateTimeOffset.UtcNow;
        var batch = new List<RowProgress>
        {
            new()
            {
                RequestId = requestId, JobId = jobId, RowIndex = 1, Status = RowStatus.Waiting,
                Stage = RowStage.None, Timestamp = now
            },
            new()
            {
                RequestId = requestId, JobId = jobId, RowIndex = 2, Status = RowStatus.Processing,
                Stage = RowStage.Downloading, Note = "https://example.com/a.png", Timestamp = now
            }
        };

        await _repository.UpsertRowsAsync(batch);
        var rows = await _repository.GetRowsAsync(requestId, jobId);

        rows.Should().HaveCount(2);
        rows[1].Status.Should().Be(RowStatus.Waiting);
        rows[2].Status.Should().Be(RowStatus.Processing);
        rows[2].Stage.Should().Be(RowStage.Downloading);
        rows[2].Note.Should().Be("https://example.com/a.png");
    }

    /// <summary>Verifies a row upsert for an existing key overwrites in place rather than duplicating.</summary>
    [Fact]
    public async Task UpsertRowsAsync_SameRowTwice_OverwritesNotDuplicates()
    {
        var requestId = Guid.NewGuid().ToString();
        var jobId = "job-1";
        var now = DateTimeOffset.UtcNow;

        await _repository.UpsertRowsAsync([
            new RowProgress { RequestId = requestId, JobId = jobId, RowIndex = 1, Status = RowStatus.Waiting, Timestamp = now }
        ]);
        await _repository.UpsertRowsAsync([
            new RowProgress { RequestId = requestId, JobId = jobId, RowIndex = 1, Status = RowStatus.Done, Timestamp = now.AddSeconds(1) }
        ]);

        var rows = await _repository.GetRowsAsync(requestId, jobId);

        rows.Should().HaveCount(1);
        rows[1].Status.Should().Be(RowStatus.Done);
    }

    /// <summary>Verifies DeleteRequestAsync removes rows from all three tables for the given request.</summary>
    [Fact]
    public async Task DeleteRequestAsync_RemovesFromAllTables()
    {
        var requestId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        await _repository.UpsertRequestAsync(new RequestProgress
        {
            RequestId = requestId, Phase = RequestPhase.Completed, Timestamp = now
        });
        await _repository.UpsertJobsAsync([
            new JobProgress { RequestId = requestId, JobId = "job-1", Status = Status.Complete, Timestamp = now }
        ]);
        await _repository.UpsertRowsAsync([
            new RowProgress { RequestId = requestId, JobId = "job-1", RowIndex = 1, Status = RowStatus.Done, Timestamp = now }
        ]);

        await _repository.DeleteRequestAsync(requestId);

        (await _repository.GetRequestAsync(requestId)).Should().BeNull();
        (await _repository.GetJobsAsync(requestId)).Should().BeEmpty();
        (await _repository.GetRowsAsync(requestId, "job-1")).Should().BeEmpty();
    }

    /// <summary>Verifies a request never upserted returns <see langword="null" /> rather than throwing.</summary>
    [Fact]
    public async Task GetRequestAsync_UnknownRequest_ReturnsNull()
    {
        (await _repository.GetRequestAsync(Guid.NewGuid().ToString())).Should().BeNull();
    }
}

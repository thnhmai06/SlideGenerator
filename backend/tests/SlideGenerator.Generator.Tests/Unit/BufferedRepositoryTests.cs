/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: BufferedRepositoryTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Generator.Persistence;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="BufferedRepository{TKey,TValue}" />'s coalesce/flush behavior, using a
///     minimal in-memory test double instead of SQLite.
/// </summary>
public sealed class BufferedRepositoryTests
{
    private sealed class FakeRepository() : BufferedRepository<string, int>(NullLogger.Instance)
    {
        public readonly List<IReadOnlyList<int>> UpsertedBatches = [];

        protected override Task UpsertBatchAsync(IReadOnlyList<int> batch, CancellationToken ct)
        {
            UpsertedBatches.Add(batch);
            return Task.CompletedTask;
        }
    }

    /// <summary>Verifies that an explicit <c>FlushAsync</c> with nothing enqueued does not call <c>UpsertBatchAsync</c>.</summary>
    [Fact]
    public async Task FlushAsync_NothingEnqueued_DoesNotUpsert()
    {
        await using var repo = new FakeRepository();

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().BeEmpty();
    }

    /// <summary>Verifies that enqueued values are persisted as a single batch on flush.</summary>
    [Fact]
    public async Task FlushAsync_MultipleKeysEnqueued_UpsertsOneBatch()
    {
        await using var repo = new FakeRepository();
        repo.Enqueue("a", 1);
        repo.Enqueue("b", 2);

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().ContainSingle();
        repo.UpsertedBatches[0].Should().BeEquivalentTo([1, 2]);
    }

    /// <summary>Verifies that the last write for a given key wins — coalescing, not appending.</summary>
    [Fact]
    public async Task Enqueue_SameKeyTwice_LastWriteWins()
    {
        await using var repo = new FakeRepository();
        repo.Enqueue("a", 1);
        repo.Enqueue("a", 2);

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().ContainSingle();
        repo.UpsertedBatches[0].Should().Equal(2);
    }

    /// <summary>Verifies that <c>Flushed</c> fires with the exact batch that was just persisted.</summary>
    [Fact]
    public async Task FlushAsync_Succeeds_RaisesFlushedWithBatch()
    {
        await using var repo = new FakeRepository();
        IReadOnlyList<int>? received = null;
        repo.Flushed += batch => received = batch;
        repo.Enqueue("a", 42);

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        received.Should().Equal(42);
    }

    /// <summary>Verifies that a second flush with nothing newly enqueued does not re-persist the previous batch.</summary>
    [Fact]
    public async Task FlushAsync_CalledTwiceWithNoNewData_SecondCallUpsertsNothing()
    {
        await using var repo = new FakeRepository();
        repo.Enqueue("a", 1);
        await repo.FlushAsync(TestContext.Current.CancellationToken);

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().ContainSingle();
    }
}

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
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>Verifies that an explicit <c>FlushAsync</c> with nothing enqueued does not call <c>UpsertBatchAsync</c>.</summary>
    [Fact]
    public async Task FlushAsync_NothingEnqueued_DoesNotUpsert()
    {
        await using var repo = new FakeRepository();

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().BeEmpty();
    }

    /// <summary>Verifies that enqueued values are persisted as a single batch on flushes.</summary>
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

    /// <summary>Verifies that the last writer for a given key wins — coalescing, not appending.</summary>
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

    /// <summary>
    ///     Verifies that concurrent flushes are serialized: while the first upsert is blocked, the second flush
    ///     cannot start its own upsert, and once released the batches arrive in the order they were drained.
    /// </summary>
    [Fact]
    public async Task FlushAsync_TwoConcurrentCalls_UpsertsAreSerializedInDrainOrder()
    {
        await using var repo = new FakeRepository();
        var firstUpsertEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstUpsert = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var upsertCount = 0;
        repo.OnUpsert = async _ =>
        {
            if (Interlocked.Increment(ref upsertCount) != 1) return;
            firstUpsertEntered.TrySetResult();
            await releaseFirstUpsert.Task.ConfigureAwait(false);
        };

        repo.Enqueue("a", 1);
        var firstFlush = repo.FlushAsync(TestContext.Current.CancellationToken);
        await firstUpsertEntered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        repo.Enqueue("a", 2);
        var secondFlush = repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().ContainSingle("the second flush must wait for the first to finish");
        repo.ConcurrentUpsertsMax.Should().Be(1);

        releaseFirstUpsert.TrySetResult();
        await Task.WhenAll(firstFlush, secondFlush).WaitAsync(Timeout, TestContext.Current.CancellationToken);

        repo.UpsertedBatches.Should().HaveCount(2);
        repo.UpsertedBatches[0].Should().Equal(1);
        repo.UpsertedBatches[1].Should().Equal(2);
    }

    /// <summary>
    ///     Verifies that a failed upsert keeps its drained values for the next flush instead of dropping the batch,
    ///     and that <c>Flushed</c> is not raised for the failed attempt.
    /// </summary>
    [Fact]
    public async Task FlushAsync_UpsertFails_KeepsTheValuesForTheNextFlush()
    {
        await using var repo = new FakeRepository { FailNext = true };
        var flushedCount = 0;
        repo.Flushed += _ => flushedCount++;
        repo.Enqueue("a", 1);

        var failingFlush = () => repo.FlushAsync(TestContext.Current.CancellationToken);
        await failingFlush.Should().ThrowAsync<InvalidOperationException>();

        flushedCount.Should().Be(0);

        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches[^1].Should().Equal(1);
        flushedCount.Should().Be(1);
    }

    /// <summary>
    ///     Verifies that when a newer value is enqueued while a failing upsert is in flight, restoring the drained
    ///     value does not overwrite the newer one — the next flush persists exactly the newer value.
    /// </summary>
    [Fact]
    public async Task FlushAsync_UpsertFailsWhileNewerValueEnqueued_NewerValueWins()
    {
        await using var repo = new FakeRepository { FailNext = true };
        repo.OnUpsert = _ =>
        {
            repo.Enqueue("a", 2);
            return Task.CompletedTask;
        };
        repo.Enqueue("a", 1);

        var failingFlush = () => repo.FlushAsync(TestContext.Current.CancellationToken);
        await failingFlush.Should().ThrowAsync<InvalidOperationException>();

        repo.OnUpsert = null;
        await repo.FlushAsync(TestContext.Current.CancellationToken);

        repo.UpsertedBatches[^1].Should().Equal(2);
    }

    private sealed class FakeRepository() : BufferedRepository<string, int>(NullLogger.Instance)
    {
        private readonly Lock _lock = new();
        private int _concurrentUpserts;

        public readonly List<IReadOnlyList<int>> UpsertedBatches = [];

        /// <summary>Highest number of <c>UpsertBatchAsync</c> calls that ever ran at the same time.</summary>
        public int ConcurrentUpsertsMax { get; private set; }

        /// <summary>Optional hook awaited inside <c>UpsertBatchAsync</c>, after the batch is recorded.</summary>
        public Func<IReadOnlyList<int>, Task>? OnUpsert { get; set; }

        /// <summary>When <see langword="true" />, the next upsert throws <see cref="InvalidOperationException" />.</summary>
        public bool FailNext { get; set; }

        protected override async Task UpsertBatchAsync(IReadOnlyList<int> batch, CancellationToken ct)
        {
            var concurrent = Interlocked.Increment(ref _concurrentUpserts);
            try
            {
                lock (_lock)
                {
                    UpsertedBatches.Add(batch);
                    ConcurrentUpsertsMax = Math.Max(ConcurrentUpsertsMax, concurrent);
                }

                if (OnUpsert is not null)
                {
                    await OnUpsert(batch).ConfigureAwait(false);
                }

                if (FailNext)
                {
                    FailNext = false;
                    throw new InvalidOperationException("Simulated upsert failure.");
                }
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentUpserts);
            }
        }
    }
}
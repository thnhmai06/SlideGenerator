/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator.Tests
 * File: PoolTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Coordinator.Application.Abstractions;
using Xunit;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace SlideGenerator.Coordinator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Pool{T}" />, verifying creation-on-demand, free-list reuse,
///     blocking and release semantics, scale-down disposal, and cleanup on disposal.
/// </summary>
public sealed class PoolTests
{
    #region AdjustAsync

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.AdjustAsync" /> disposes free instances that exceed
    ///     the current limit immediately, without waiting for a release.
    /// </summary>
    [Fact]
    public async Task AdjustAsync_ExcessFreeInstances_DisposesThem()
    {
        var limit = 3u;
        using var pool = new StubPool(() => new Resource(), () => limit);

        var instances = await Task.WhenAll(
            Enumerable.Range(0, 3).Select(_ => pool.AcquireAsync(TestContext.Current.CancellationToken).AsTask()));
        foreach (var i in instances) pool.Release(i);

        limit = 1u;
        await pool.AdjustAsync();

        instances.Count(i => i.IsDisposed).Should().Be(2);
    }

    #endregion

    #region Test doubles

    /// <summary>
    ///     Minimal <see cref="IDisposable" /> implementation used as the pooled resource.
    /// </summary>
    private sealed class Resource : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    /// <summary>
    ///     Concrete subclass that exposes <see cref="Pool{T}" />'s protected members for testing.
    /// </summary>
    private sealed class StubPool(Func<Resource> factory, Func<uint> limit)
        : Pool<Resource>(factory, limit)
    {
        /// <inheritdoc cref="Pool{T}.AcquireAsync" />
        public new ValueTask<Resource> AcquireAsync(CancellationToken ct = default)
        {
            return base.AcquireAsync(ct);
        }

        /// <inheritdoc cref="Pool{T}.Release" />
        public new void Release(Resource instance)
        {
            base.Release(instance);
        }
    }

    #endregion

    #region AcquireAsync

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.AcquireAsync" /> invokes the factory when the pool is empty.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_PoolEmpty_CallsFactory()
    {
        var calls = 0;
        using var pool = new StubPool(() =>
        {
            calls++;
            return new Resource();
        }, () => 5);

        await pool.AcquireAsync(TestContext.Current.CancellationToken);

        calls.Should().Be(1);
    }

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.AcquireAsync" /> reuses a previously released instance
    ///     without invoking the factory a second time.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_InstanceReleased_ReusesWithoutFactory()
    {
        var calls = 0;
        using var pool = new StubPool(() =>
        {
            calls++;
            return new Resource();
        }, () => 5);

        var instance = await pool.AcquireAsync(TestContext.Current.CancellationToken);
        pool.Release(instance);
        await pool.AcquireAsync(TestContext.Current.CancellationToken);

        calls.Should().Be(1);
    }

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.AcquireAsync" /> waits asynchronously when at the limit
    ///     and resumes only after an in-use instance is released.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_AtLimit_BlocksUntilRelease()
    {
        using var pool = new StubPool(() => new Resource(), () => 1);
        var first = await pool.AcquireAsync(TestContext.Current.CancellationToken);

        var secondTask = pool.AcquireAsync(TestContext.Current.CancellationToken).AsTask();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        secondTask.IsCompleted.Should().BeFalse();

        pool.Release(first);
        await secondTask.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        secondTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that a limit of zero is treated as unlimited: all concurrent acquires complete
    ///     without blocking, and the factory is called for each.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LimitZero_CreatesUnlimited()
    {
        var calls = 0;
        using var pool = new StubPool(() =>
        {
            Interlocked.Increment(ref calls);
            return new Resource();
        }, () => 0);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => pool.AcquireAsync(TestContext.Current.CancellationToken).AsTask()));

        calls.Should().Be(5);
        results.Should().HaveCount(5).And.AllSatisfy(r => r.Should().NotBeNull());
    }

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.AcquireAsync" /> throws <see cref="OperationCanceledException" />
    ///     when the supplied token is already canceled and no instance is available.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var pool = new StubPool(() => new Resource(), () => 1);
        await pool.AcquireAsync(TestContext.Current.CancellationToken); // occupy only slot

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await FluentActions
            .Awaiting(() => pool.AcquireAsync(cts.Token).AsTask())
            .Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.AcquireAsync" /> throws <see cref="ObjectDisposedException" />
    ///     after the pool has been disposed.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var pool = new StubPool(() => new Resource(), () => 5);
        pool.Dispose();

        await FluentActions
            .Awaiting(() => pool.AcquireAsync(TestContext.Current.CancellationToken).AsTask())
            .Should().ThrowAsync<ObjectDisposedException>();
    }

    /// <summary>
    ///     Verifies that when the factory throws, <see cref="Pool{T}.AcquireAsync" /> decrements
    ///     the total count so a subsequent acquiring can succeed (slot not leaked).
    /// </summary>
    [Fact]
    public async Task AcquireAsync_FactoryThrows_TotalDecrementedSoNextAcquireSucceeds()
    {
        var calls = 0;
        using var pool = new StubPool(
            () => Interlocked.Increment(ref calls) == 1
                ? throw new InvalidOperationException("factory failed")
                : new Resource(),
            () => 1);

        await FluentActions
            .Awaiting(() => pool.AcquireAsync(TestContext.Current.CancellationToken).AsTask())
            .Should().ThrowAsync<InvalidOperationException>();

        var result = await pool.AcquireAsync(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
    }

    #endregion

    #region Release

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.Release" /> disposes the returned instance when the
    ///     total count exceeds the current limit (scale-down).
    /// </summary>
    [Fact]
    public async Task Release_OverLimit_DisposesInstance()
    {
        var limit = 2u;
        using var pool = new StubPool(() => new Resource(), () => limit);

        var first = await pool.AcquireAsync(TestContext.Current.CancellationToken);
        var second = await pool.AcquireAsync(TestContext.Current.CancellationToken);

        pool.Release(first); // limit=2, total=2 → returns to free list

        limit = 1u;
        pool.Release(second); // limit=1, total=2 → dispose second

        second.IsDisposed.Should().BeTrue();
        first.IsDisposed.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.Release" /> disposes the instance immediately when
    ///     the pool has already been disposed, rather than returning it to the free list.
    /// </summary>
    [Fact]
    public async Task Release_AfterPoolDisposed_DisposesInstance()
    {
        var pool = new StubPool(() => new Resource(), () => 5);
        var instance = await pool.AcquireAsync(TestContext.Current.CancellationToken);

        pool.Dispose();
        pool.Release(instance);

        instance.IsDisposed.Should().BeTrue();
    }

    #endregion

    #region Dispose

    /// <summary>
    ///     Verifies that <see cref="Pool{T}.Dispose" /> disposes all idle instances held in the free list.
    /// </summary>
    [Fact]
    public async Task Dispose_FreesAllFreeInstances()
    {
        var pool = new StubPool(() => new Resource(), () => 5);
        var first = await pool.AcquireAsync(TestContext.Current.CancellationToken);
        var second = await pool.AcquireAsync(TestContext.Current.CancellationToken);
        pool.Release(first);
        pool.Release(second);

        pool.Dispose();

        first.IsDisposed.Should().BeTrue();
        second.IsDisposed.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that calling <see cref="Pool{T}.Dispose" /> multiple times does not throw.
    /// </summary>
    [Fact]
    public void Dispose_Idempotent()
    {
        var pool = new StubPool(() => new Resource(), () => 5);
        pool.Dispose();

        var act = pool.Dispose;
        act.Should().NotThrow();
    }

    #endregion
}
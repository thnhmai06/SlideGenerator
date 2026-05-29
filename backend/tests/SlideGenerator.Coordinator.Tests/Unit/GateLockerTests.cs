/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator.Tests
 * File: GateLockerTests.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Coordinator.Application.Services;
using Xunit;

namespace SlideGenerator.Coordinator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="GateLocker{TGate}" />, verifying per-gate concurrency limiting,
///     blocking/release semantics, non-blocking try-acquire, cancellation propagation, and disposal cleanup.
/// </summary>
public sealed class GateLockerTests
{
    #region Helpers

    /// <summary>
    ///     Creates a <see cref="GateLocker{TGate}" /> where every gate shares the same
    ///     <paramref name="limitPerGate" /> concurrent slots.
    /// </summary>
    /// <param name="limitPerGate">The maximum number of concurrent operations for each gate.</param>
    private static GateLocker<TestGate> CreateLocker(uint limitPerGate = 2)
    {
        var logger = NullLogger<GateLocker<TestGate>>.Instance;
        return new GateLocker<TestGate>(_ => limitPerGate, logger);
    }

    #endregion

    #region Dispose

    /// <summary>
    ///     Verifies that <see cref="GateLocker{TGate}.Dispose" /> cancels all pending waiters, causing their
    ///     associated tasks to fault with <see cref="OperationCanceledException" />.
    /// </summary>
    [Fact]
    public async Task Dispose_WithPendingWaiters_CancelsAllWaiters()
    {
        var locker = CreateLocker(1);
        await locker.AcquireAsync(TestGate.A, TestContext.Current.CancellationToken);

        var waiter = locker.AcquireAsync(TestGate.A, TestContext.Current.CancellationToken).AsTask();
        waiter.IsCompleted.Should().BeFalse();

        locker.Dispose();

        await FluentActions.Awaiting(() => waiter)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    /// <summary>Minimal gate enum used exclusively within these tests.</summary>
    private enum TestGate
    {
        A,
        B,
        C,
        D,
        E
    }

    #region AcquireAsync

    /// <summary>
    ///     Verifies that <see cref="GateLocker{TGate}.AcquireAsync" /> completes immediately when the number
    ///     of concurrent holders has not yet reached the configured limit.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LimitNotReached_CompletesImmediately()
    {
        using var locker = CreateLocker();

        var first = locker.AcquireAsync(TestGate.B, TestContext.Current.CancellationToken);
        var second = locker.AcquireAsync(TestGate.B, TestContext.Current.CancellationToken);

        await first;
        await second; // both within limit — should not block
    }

    /// <summary>
    ///     Verifies that a second call to <see cref="GateLocker{TGate}.AcquireAsync" /> blocks while the gate is
    ///     at capacity, and completes only after <see cref="GateLocker{TGate}.Release" /> is invoked by the holder.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LimitReached_BlocksUntilRelease()
    {
        using var locker = CreateLocker(1);

        await locker.AcquireAsync(TestGate.B, TestContext.Current.CancellationToken);

        var secondAcquire = locker.AcquireAsync(TestGate.B, TestContext.Current.CancellationToken).AsTask();
        secondAcquire.IsCompleted.Should().BeFalse();

        locker.Release(TestGate.B);

        await secondAcquire.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        secondAcquire.IsCompletedSuccessfully.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="GateLocker{TGate}.AcquireAsync" /> throws <see cref="OperationCanceledException" />
    ///     immediately when the supplied <see cref="CancellationToken" /> is already canceled at call time.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        using var locker = CreateLocker(1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => locker.AcquireAsync(TestGate.B, cts.Token).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that a pending waiter in <see cref="GateLocker{TGate}.AcquireAsync" /> is unblocked with
    ///     <see cref="OperationCanceledException" /> when its <see cref="CancellationToken" /> is canceled
    ///     while the gate is at capacity.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_TokenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        using var locker = CreateLocker(1);
        await locker.AcquireAsync(TestGate.B, TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        var waitingTask = locker.AcquireAsync(TestGate.B, cts.Token).AsTask();

        await cts.CancelAsync();

        await FluentActions.Awaiting(() => waitingTask)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region TryAcquire

    /// <summary>
    ///     Verifies that <see cref="GateLocker{TGate}.TryAcquire" /> returns <see langword="true" /> when the
    ///     gate has available capacity, acquiring a slot immediately without blocking.
    /// </summary>
    [Fact]
    public void TryAcquire_LimitNotReached_ReturnsTrue()
    {
        using var locker = CreateLocker();

        var result = locker.TryAcquire(TestGate.C);

        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="GateLocker{TGate}.TryAcquire" /> returns <see langword="false" /> when the
    ///     gate is at full capacity, leaving the gate state unchanged.
    /// </summary>
    [Fact]
    public async Task TryAcquire_LimitReached_ReturnsFalse()
    {
        using var locker = CreateLocker(1);
        await locker.AcquireAsync(TestGate.C, TestContext.Current.CancellationToken);

        var result = locker.TryAcquire(TestGate.C);

        result.Should().BeFalse();
    }

    #endregion

    #region Release

    /// <summary>
    ///     Verifies that <see cref="GateLocker{TGate}.Release" /> does not throw when called for a gate that
    ///     was never acquired — the method is intentionally idempotent for safety.
    /// </summary>
    [Fact]
    public void Release_GateNeverAcquired_DoesNotThrow()
    {
        using var locker = CreateLocker();

        var act = () => locker.Release(TestGate.D);

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Verifies that calling <see cref="GateLocker{TGate}.Release" /> after a successful
    ///     <see cref="GateLocker{TGate}.AcquireAsync" /> allows the next waiting caller to be admitted.
    /// </summary>
    [Fact]
    public async Task Release_AfterAcquire_AdmitsNextWaiter()
    {
        using var locker = CreateLocker(1);
        await locker.AcquireAsync(TestGate.D, TestContext.Current.CancellationToken);

        var waiter = locker.AcquireAsync(TestGate.D, TestContext.Current.CancellationToken).AsTask();
        waiter.IsCompleted.Should().BeFalse();

        locker.Release(TestGate.D);

        await waiter.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        waiter.IsCompletedSuccessfully.Should().BeTrue();
    }

    #endregion
}
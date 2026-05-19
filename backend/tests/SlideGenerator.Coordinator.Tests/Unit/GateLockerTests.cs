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
using NSubstitute;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Coordinator.Infrastructure.Services;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using Xunit;

namespace SlideGenerator.Coordinator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="GateLocker" />, verifying per-gate concurrency limiting, blocking/release
///     semantics, non-blocking try-acquire, cancellation propagation, and disposal cleanup.
/// </summary>
public sealed class GateLockerTests
{
    #region Helpers

    /// <summary>
    ///     Creates a <see cref="GateLocker" /> backed by mocked dependencies, with all gates set to
    ///     <paramref name="limitPerGate" /> concurrent slots.
    /// </summary>
    /// <param name="limitPerGate">The maximum number of concurrent operations for each gate type.</param>
    private static GateLocker CreateLocker(int limitPerGate = 2)
    {
        var setting = new Setting
        {
            Performance = new Setting.PerformanceSetting
            {
                MaxParallelDownloadImage = limitPerGate,
                MaxParallelEditImage = limitPerGate,
                MaxParallelEditPresentation = limitPerGate,
                MaxParallelReadWorkbook = limitPerGate,
                MaxParallelReadPresentation = limitPerGate
            }
        };
        var provider = Substitute.For<ISettingProvider>();
        provider.Current.Returns(setting);
        var logger = Substitute.For<ISystemLogger>();
        return new GateLocker(provider, logger);
    }

    #endregion

    #region Dispose

    /// <summary>
    ///     Verifies that <see cref="GateLocker.Dispose" /> cancels all pending waiters, causing their
    ///     associated tasks to fault with <see cref="OperationCanceledException" />.
    /// </summary>
    [Fact]
    public async Task Dispose_WithPendingWaiters_CancelsAllWaiters()
    {
        var locker = CreateLocker(1);
        await locker.AcquireAsync(GateType.EditPresentation, TestContext.Current.CancellationToken); // fill the slot

        var waiter = locker.AcquireAsync(GateType.EditPresentation, TestContext.Current.CancellationToken).AsTask();
        waiter.IsCompleted.Should().BeFalse();

        locker.Dispose();

        await FluentActions.Awaiting(() => waiter)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region AcquireAsync

    /// <summary>
    ///     Verifies that <see cref="GateLocker.AcquireAsync" /> completes immediately when the number
    ///     of concurrent holders has not yet reached the configured limit.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LimitNotReached_CompletesImmediately()
    {
        using var locker = CreateLocker();

        var first = locker.AcquireAsync(GateType.DownloadImage, TestContext.Current.CancellationToken);
        var second = locker.AcquireAsync(GateType.DownloadImage, TestContext.Current.CancellationToken);

        await first;
        await second; // both within limit — should not block
    }

    /// <summary>
    ///     Verifies that a second call to <see cref="GateLocker.AcquireAsync" /> blocks while the gate is
    ///     at capacity, and completes only after <see cref="GateLocker.Release" /> is invoked by the holder.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LimitReached_BlocksUntilRelease()
    {
        using var locker = CreateLocker(1);

        await locker.AcquireAsync(GateType.DownloadImage,
            TestContext.Current.CancellationToken); // fills the single slot

        var secondAcquire = locker.AcquireAsync(GateType.DownloadImage, TestContext.Current.CancellationToken).AsTask();
        secondAcquire.IsCompleted.Should().BeFalse(); // must be pending

        locker.Release(GateType.DownloadImage);

        await secondAcquire.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        secondAcquire.IsCompletedSuccessfully.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="GateLocker.AcquireAsync" /> throws <see cref="OperationCanceledException" />
    ///     immediately when the supplied <see cref="CancellationToken" /> is already canceled at call time.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_AlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        using var locker = CreateLocker(1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => locker.AcquireAsync(GateType.DownloadImage, cts.Token).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    ///     Verifies that a pending waiter in <see cref="GateLocker.AcquireAsync" /> is unblocked with
    ///     <see cref="OperationCanceledException" /> when its <see cref="CancellationToken" /> is canceled
    ///     while the gate is at capacity.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_TokenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        using var locker = CreateLocker(1);
        await locker.AcquireAsync(GateType.DownloadImage, TestContext.Current.CancellationToken); // fill the slot

        using var cts = new CancellationTokenSource();
        var waitingTask = locker.AcquireAsync(GateType.DownloadImage, cts.Token).AsTask();

        await cts.CancelAsync();

        await FluentActions.Awaiting(() => waitingTask)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region TryAcquire

    /// <summary>
    ///     Verifies that <see cref="GateLocker.TryAcquire" /> returns <see langword="true" /> when the
    ///     gate has available capacity, acquiring a slot immediately without blocking.
    /// </summary>
    [Fact]
    public void TryAcquire_LimitNotReached_ReturnsTrue()
    {
        using var locker = CreateLocker();

        var result = locker.TryAcquire(GateType.EditImage);

        result.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="GateLocker.TryAcquire" /> returns <see langword="false" /> when the
    ///     gate is at full capacity, leaving the gate state unchanged.
    /// </summary>
    [Fact]
    public async Task TryAcquire_LimitReached_ReturnsFalse()
    {
        using var locker = CreateLocker(1);
        await locker.AcquireAsync(GateType.EditImage, TestContext.Current.CancellationToken); // fill the single slot

        var result = locker.TryAcquire(GateType.EditImage);

        result.Should().BeFalse();
    }

    #endregion

    #region Release

    /// <summary>
    ///     Verifies that <see cref="GateLocker.Release" /> does not throw when called for a gate that
    ///     was never acquired — the method is intentionally idempotent for safety.
    /// </summary>
    [Fact]
    public void Release_GateNeverAcquired_DoesNotThrow()
    {
        using var locker = CreateLocker();

        var act = () => locker.Release(GateType.ReadWorkbook);

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Verifies that calling <see cref="GateLocker.Release" /> after a successful
    ///     <see cref="GateLocker.AcquireAsync" /> allows the next waiting caller to be admitted.
    /// </summary>
    [Fact]
    public async Task Release_AfterAcquire_AdmitsNextWaiter()
    {
        using var locker = CreateLocker(1);
        await locker.AcquireAsync(GateType.ReadWorkbook, TestContext.Current.CancellationToken);

        var waiter = locker.AcquireAsync(GateType.ReadWorkbook, TestContext.Current.CancellationToken).AsTask();
        waiter.IsCompleted.Should().BeFalse();

        locker.Release(GateType.ReadWorkbook);

        await waiter.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        waiter.IsCompletedSuccessfully.Should().BeTrue();
    }

    #endregion
}
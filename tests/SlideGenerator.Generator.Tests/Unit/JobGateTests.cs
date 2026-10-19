/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: JobGateTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Jobs;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="JobGate" />, the shared cap on concurrently running jobs: slot sharing, release,
///     and the swap-not-resize behavior when the limit changes.
/// </summary>
public sealed class JobGateTests
{
    private static readonly TimeSpan NoProgressWindow = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(3);

    private static async Task<bool> CompletesWithinAsync(Task task, TimeSpan timeout)
    {
        return await Task.WhenAny(task, Task.Delay(timeout)) == task;
    }

    /// <summary>A job beyond the cap waits until a running job releases its slot.</summary>
    [Fact]
    public async Task AcquireAsync_BeyondTheCap_WaitsForARelease()
    {
        var gate = new JobGate(() => 1);
        var first = await gate.AcquireAsync(TestContext.Current.CancellationToken);

        var second = gate.AcquireAsync(TestContext.Current.CancellationToken);
        (await CompletesWithinAsync(second, NoProgressWindow)).Should().BeFalse("the only slot is taken");

        first.Dispose();
        using var lease = await second.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
    }

    /// <summary>Disposing a lease twice releases the slot only once.</summary>
    [Fact]
    public async Task Lease_DisposedTwice_ReleasesOnlyOnce()
    {
        var gate = new JobGate(() => 1);
        var first = await gate.AcquireAsync(TestContext.Current.CancellationToken);
        first.Dispose();
        first.Dispose();

        using var second = await gate.AcquireAsync(TestContext.Current.CancellationToken);
        var third = gate.AcquireAsync(TestContext.Current.CancellationToken);

        (await CompletesWithinAsync(third, NoProgressWindow)).Should().BeFalse("only one slot exists");
    }

    /// <summary>
    ///     Raising the limit swaps in a new semaphore: a job started afterwards runs immediately even though an
    ///     earlier job still holds the only slot of the old one, and releasing the old lease cannot overflow it.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_AfterLimitIncrease_IsIndependentOfJobsOnTheOldSemaphore()
    {
        var limit = 1;
        var gate = new JobGate(() => limit);
        var old = await gate.AcquireAsync(TestContext.Current.CancellationToken);

        limit = 5;
        using var fresh = await gate.AcquireAsync(TestContext.Current.CancellationToken)
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);

        var release = () => old.Dispose();
        release.Should().NotThrow();
    }

    /// <summary>A limit below 1 is treated as 1 rather than throwing.</summary>
    [Fact]
    public async Task AcquireAsync_LimitBelowOne_IsTreatedAsOne()
    {
        var gate = new JobGate(() => 0);

        using var lease = await gate.AcquireAsync(TestContext.Current.CancellationToken)
            .WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
    }

    /// <summary>Cancelling while queued for a slot throws instead of acquiring.</summary>
    [Fact]
    public async Task AcquireAsync_CancelledWhileQueued_Throws()
    {
        var gate = new JobGate(() => 1);
        using var held = await gate.AcquireAsync(TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();

        var waiting = gate.AcquireAsync(cts.Token);
        await cts.CancelAsync();

        var act = async () => await waiting;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

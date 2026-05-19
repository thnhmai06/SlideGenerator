/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator.Tests
 * File: CoordinatorRobustnessTests.cs
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
using SlideGenerator.Coordinator.Domain.Models;
using Xunit;
using CoordinatorImpl = SlideGenerator.Coordinator.Application.Services.Coordinator;

namespace SlideGenerator.Coordinator.Tests.Unit;

/// <summary>
///     Robustness tests for <see cref="CoordinatorImpl" /> covering edge cases not exercised by
///     <c>CoordinatorTests</c>: what happens to <see cref="SecondaryEnlistment" /> waiters when
///     the primary never reports a result (e.g. crash, infinite loop, or step skip).
/// </summary>
public sealed class CoordinatorRobustnessTests
{
    #region BUG / design gap — primary never signals

    /// <summary>
    ///     BUG (HIGH, design gap): <see cref="CoordinatorImpl.Enlist" /> hands out
    ///     <see cref="SecondaryEnlistment.WaitTask" /> backed by a <see cref="TaskCompletionSource{TResult}" />
    ///     that the primary is contractually obliged to complete. There is
    ///     <b>
    ///         no timeout, no
    ///         cancellation, no faulting path
    ///     </b>
    ///     : if the primary crashes before calling
    ///     <see cref="PrimaryEnlistment.SubmitResult" />, every secondary's <c>WaitTask</c>
    ///     is observably hung forever. Inside the generating workflow this manifests as a
    ///     permanently stuck Phase B/C barrier.
    ///     <para>
    ///         This test verifies the consequence concretely: after the primary fails to submit,
    ///         the secondary's task is still <see cref="TaskStatus.WaitingForActivation" /> well
    ///         past a generous wait window. The correct fix is to expose either a cancellation
    ///         token on <c>Enlist</c> or a fault-propagation channel for the primary.
    ///     </para>
    /// </summary>
    [Fact(DisplayName = "BUG: secondary WaitTask hangs forever if primary never SubmitResult")]
    public async Task Enlist_PrimaryNeverSubmits_SecondaryWaitTaskHangsIndefinitely()
    {
        var coordinator = new CoordinatorImpl();
        _ = (PrimaryEnlistment)coordinator.Enlist("orphan-key");
        var secondary = (SecondaryEnlistment)coordinator.Enlist("orphan-key");

        // Simulate "primary crashed" — we deliberately do not call SubmitResult.
        var completed = await Task.WhenAny(secondary.WaitTask, Task.Delay(250, TestContext.Current.CancellationToken))
            .ConfigureAwait(false);

        completed.Should().NotBeSameAs(secondary.WaitTask,
            "WaitTask must not complete when the primary never submits — proves the design gap");
        secondary.WaitTask.IsCompleted.Should().BeFalse(
            "no timeout/cancellation channel exists today; fix should expose one");
    }

    /// <summary>
    ///     Contract: <see cref="PrimaryEnlistment" /> must expose a fault-propagation channel
    ///     (<c>SubmitException</c> property of type <see cref="Action{Exception}" />) so that
    ///     secondaries can observe failure rather than hanging forever on a null result.
    /// </summary>
    [Fact(DisplayName = "PrimaryEnlistment exposes SubmitException to fault secondary waiters")]
    public async Task PrimaryEnlistment_HasNoChannelToReportException()
    {
        var coordinator = new CoordinatorImpl();
        var primary = (PrimaryEnlistment)coordinator.Enlist("k");
        var secondary = (SecondaryEnlistment)coordinator.Enlist("k");

        // SubmitException is a positional property on the record — verify via reflection
        var prop = primary.GetType().GetProperty("SubmitException");
        prop.Should().NotBeNull("PrimaryEnlistment must expose SubmitException to fault secondaries");

        // Functional: calling SubmitException must fault the secondary's WaitTask
        var expected = new InvalidOperationException("primary failed");
        primary.SubmitException(expected);

        await secondary.WaitTask.Awaiting(t => t).Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("primary failed");
    }

    #endregion

    #region Race — secondary enlisting after primary already completed

    /// <summary>
    ///     Verifies the late-joiner happy path: a caller that enlists for a key
    ///     <b>after</b> the primary has already submitted a result still observes the
    ///     completed task immediately. Locks down the contract that the result is cached
    ///     for the lifetime of the coordinator instance.
    /// </summary>
    [Fact]
    public async Task Enlist_SecondaryAfterPrimaryAlreadySubmitted_ObservesResultImmediately()
    {
        var coordinator = new CoordinatorImpl();
        var primary = (PrimaryEnlistment)coordinator.Enlist("k");
        primary.SubmitResult("/early/result.pptx");

        var late = (SecondaryEnlistment)coordinator.Enlist("k");

        late.WaitTask.IsCompletedSuccessfully.Should().BeTrue();
        (await late.WaitTask).Should().Be("/early/result.pptx");
    }

    /// <summary>
    ///     Verifies that calling <see cref="PrimaryEnlistment.SubmitResult" /> twice does not
    ///     throw and does not overwrite the first value — the underlying TCS uses
    ///     <c>TrySetResult</c>. Locks down the idempotency contract.
    /// </summary>
    [Fact]
    public async Task PrimarySubmitResult_CalledTwice_FirstValueWins()
    {
        var coordinator = new CoordinatorImpl();
        var primary = (PrimaryEnlistment)coordinator.Enlist("k");
        var secondary = (SecondaryEnlistment)coordinator.Enlist("k");

        primary.SubmitResult("/first.pptx");
        var act = () => primary.SubmitResult("/second.pptx");

        act.Should().NotThrow();
        (await secondary.WaitTask).Should().Be("/first.pptx");
    }

    #endregion
}
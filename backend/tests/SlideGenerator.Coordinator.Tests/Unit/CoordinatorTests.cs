/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator.Tests
 * File: CoordinatorTests.cs
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

using System.Collections.Concurrent;
using FluentAssertions;
using SlideGenerator.Coordinator.Domain.Models;
using Xunit;
using Coordinator = SlideGenerator.Coordinator.Application.Services.Coordinator;
using CoordinatorImpl = SlideGenerator.Coordinator.Application.Services.Coordinator;

namespace SlideGenerator.Coordinator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Coordinator" />, verifying the primary/secondary enlistment contract,
///     result propagation, and race-condition safety when multiple callers enlist the same key concurrently.
/// </summary>
public sealed class CoordinatorTests
{
    #region Enlist — Concurrency

    /// <summary>
    ///     Verifies that under concurrent enlistment of the same key from many threads,
    ///     exactly one <see cref="OwnerEnlistment" /> is produced and all others become
    ///     <see cref="WaiterEnlistment" /> instances. This confirms the <c>TryAdd</c> atomicity guarantee.
    /// </summary>
    [Fact]
    public async Task Enlist_ConcurrentCallersForSameKey_ExactlyOnePrimary()
    {
        var coordinator = new CoordinatorImpl();
        var enlistments = new ConcurrentBag<Enlistment>();

        await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => enlistments.Add(coordinator.Enlist("race-key")))));

        enlistments.OfType<OwnerEnlistment>().Should().HaveCount(1);
        enlistments.OfType<WaiterEnlistment>().Should().HaveCount(49);
    }

    #endregion

    #region Enlist — Basic Role Assignment

    /// <summary>
    ///     Verifies that the first caller to <see cref="Coordinator.Enlist" /> for a given key
    ///     receives a <see cref="OwnerEnlistment" />, marking it as the owner of that operation.
    /// </summary>
    [Fact]
    public void Enlist_FirstCallerForKey_ReturnsOwnerEnlistment()
    {
        var coordinator = new CoordinatorImpl();

        var enlistment = coordinator.Enlist("key-1");

        enlistment.Should().BeOfType<OwnerEnlistment>();
    }

    /// <summary>
    ///     Verifies that the second caller to <see cref="Coordinator.Enlist" /> for the same key
    ///     receives a <see cref="WaiterEnlistment" />, which awaits the primary's result.
    /// </summary>
    [Fact]
    public void Enlist_SecondCallerForSameKey_ReturnsWaiterEnlistment()
    {
        var coordinator = new CoordinatorImpl();
        coordinator.Enlist("key-1");

        var enlistment = coordinator.Enlist("key-1");

        enlistment.Should().BeOfType<WaiterEnlistment>();
    }

    /// <summary>
    ///     Verifies that all further callers (beyond the second) for the same key also receive
    ///     <see cref="WaiterEnlistment" /> instances, each awaiting the same primary result.
    /// </summary>
    [Fact]
    public void Enlist_MultipleSubsequentCallersForSameKey_AllReturnWaiterEnlistment()
    {
        var coordinator = new CoordinatorImpl();
        coordinator.Enlist("key-1"); // primary

        var second = coordinator.Enlist("key-1");
        var third = coordinator.Enlist("key-1");
        var fourth = coordinator.Enlist("key-1");

        second.Should().BeOfType<WaiterEnlistment>();
        third.Should().BeOfType<WaiterEnlistment>();
        fourth.Should().BeOfType<WaiterEnlistment>();
    }

    /// <summary>
    ///     Verifies that different keys are treated as independent operations: each unique key
    ///     produces its own <see cref="OwnerEnlistment" /> regardless of other enlisted keys.
    /// </summary>
    [Fact]
    public void Enlist_DifferentKeys_EachKeyGetsSeparatePrimary()
    {
        var coordinator = new CoordinatorImpl();

        var first = coordinator.Enlist("key-a");
        var second = coordinator.Enlist("key-b");
        var third = coordinator.Enlist("key-c");

        first.Should().BeOfType<OwnerEnlistment>();
        second.Should().BeOfType<OwnerEnlistment>();
        third.Should().BeOfType<OwnerEnlistment>();
    }

    #endregion

    #region Enlist — Result Propagation

    /// <summary>
    ///     Verifies that when the primary submits a non-null result path via
    ///     <see cref="OwnerEnlistment.SubmitResult" />, all secondaries receive that exact path value.
    /// </summary>
    [Fact]
    public async Task Enlist_PrimarySubmitsPath_SecondaryReceivesSamePath()
    {
        var coordinator = new CoordinatorImpl();
        var primary = (OwnerEnlistment)coordinator.Enlist("key-1");
        var secondary = (WaiterEnlistment)coordinator.Enlist("key-1");

        primary.SubmitResult("/output/slides.pptx");

        var result = await secondary.WaitTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.Should().Be("/output/slides.pptx");
    }

    /// <summary>
    ///     Verifies that when the primary submits <see langword="null" /> (indicating failure),
    ///     the secondary receives <see langword="null" /> as its result.
    /// </summary>
    [Fact]
    public async Task Enlist_PrimarySubmitsNull_SecondaryReceivesNull()
    {
        var coordinator = new CoordinatorImpl();
        var primary = (OwnerEnlistment)coordinator.Enlist("key-1");
        var secondary = (WaiterEnlistment)coordinator.Enlist("key-1");

        primary.SubmitResult(null);

        var result = await secondary.WaitTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that multiple secondary enlistments for the same key all receive the primary's
    ///     submitted result, regardless of how many secondaries are waiting.
    /// </summary>
    [Fact]
    public async Task Enlist_MultipleSecondaries_AllReceivePrimaryResult()
    {
        var coordinator = new CoordinatorImpl();
        var primary = (OwnerEnlistment)coordinator.Enlist("shared-key");
        var secondaries = Enumerable.Range(0, 5)
            .Select(_ => (WaiterEnlistment)coordinator.Enlist("shared-key"))
            .ToList();

        primary.SubmitResult("/shared/output.pptx");

        var results = await Task.WhenAll(secondaries.Select(s => s.WaitTask))
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        results.Should().AllBe("/shared/output.pptx");
    }

    #endregion
}
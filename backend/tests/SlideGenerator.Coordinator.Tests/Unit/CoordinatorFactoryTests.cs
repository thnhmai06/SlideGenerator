/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator.Tests
 * File: CoordinatorFactoryTests.cs
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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Application.Services;
using SlideGenerator.Coordinator.Domain.Models;
using Xunit;
using CoordinatorFactoryImpl = SlideGenerator.Coordinator.Application.Services.CoordinatorFactory;

namespace SlideGenerator.Coordinator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="CoordinatorFactory" />, verifying that each call to
///     <see cref="CoordinatorFactory.Create" /> produces a fresh, independent <see cref="ICoordinator" /> instance.
/// </summary>
public sealed class CoordinatorFactoryTests
{
    #region Create

    /// <summary>
    ///     Verifies that <see cref="CoordinatorFactory.Create" /> returns a non-null <see cref="ICoordinator" />
    ///     instance on every invocation.
    /// </summary>
    [Fact]
    public void Create_Called_ReturnsNonNullInstance()
    {
        var factory = new CoordinatorFactoryImpl();

        var coordinator = factory.Create();

        coordinator.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that successive calls to <see cref="CoordinatorFactory.Create" /> return distinct
    ///     object instances, ensuring coordinators are not reused across workflow runs.
    /// </summary>
    [Fact]
    public void Create_CalledMultipleTimes_ReturnsDistinctInstances()
    {
        var factory = new CoordinatorFactoryImpl();

        var first = factory.Create();
        var second = factory.Create();

        first.Should().NotBeSameAs(second);
    }

    /// <summary>
    ///     Verifies that a freshly created coordinator starts in an empty state: the first enlistment
    ///     for any key must be a <see cref="PrimaryEnlistment" />, not a secondary.
    /// </summary>
    [Fact]
    public void Create_FreshInstance_FirstEnlistmentIsPrimary()
    {
        var factory = new CoordinatorFactoryImpl();
        var coordinator = factory.Create();

        var enlistment = coordinator.Enlist("any-key");

        enlistment.Should().BeOfType<PrimaryEnlistment>();
    }

    /// <summary>
    ///     Verifies that within a single created coordinator, further enlistments for the same key
    ///     correctly result in <see cref="SecondaryEnlistment" /> instances.
    /// </summary>
    [Fact]
    public void Create_FreshInstance_AnotherEnlistmentIsSecondary()
    {
        var factory = new CoordinatorFactoryImpl();
        var coordinator = factory.Create();

        var firstEnlistment = coordinator.Enlist("shared-key");
        var secondEnlistment = coordinator.Enlist("shared-key");
        var thirdEnlistment = coordinator.Enlist("shared-key");

        firstEnlistment.Should().BeOfType<PrimaryEnlistment>();
        secondEnlistment.Should().BeOfType<SecondaryEnlistment>();
        thirdEnlistment.Should().BeOfType<SecondaryEnlistment>();
    }

    /// <summary>
    ///     Verifies that two coordinators from the same factory are completely independent:
    ///     enlisting the same key on each produces two separate primaries.
    /// </summary>
    [Fact]
    public void Create_TwoInstances_DoNotShareState()
    {
        var factory = new CoordinatorFactoryImpl();
        var first = factory.Create();
        var second = factory.Create();

        var enlistmentA = first.Enlist("shared-key");
        var enlistmentB = second.Enlist("shared-key");

        enlistmentA.Should().BeOfType<PrimaryEnlistment>();
        enlistmentB.Should().BeOfType<PrimaryEnlistment>();
    }

    #endregion
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: RowFilterEditorViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Recipe.Models;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="RowFilterEditorViewModel" />'s round-trip fidelity — the failure class
///     advisor flagged before any radio-group UI is built: switching mode must never silently lose data.
/// </summary>
public sealed class RowFilterEditorViewModelTests
{
    [Fact]
    public void RoundTrip_Null_LoadsAsAllAndProjectsBackToNull()
    {
        var vm = new RowFilterEditorViewModel();
        vm.LoadFrom(null);

        vm.Mode.Should().Be(RowFilterEditorMode.All);
        vm.ToRowFilter().Should().BeNull();
    }

    [Fact]
    public void RoundTrip_AllRowFilterInstance_LoadsAsAllAndProjectsBackToNull()
    {
        var vm = new RowFilterEditorViewModel();
        vm.LoadFrom(new AllRowFilter());

        vm.Mode.Should().Be(RowFilterEditorMode.All);
        vm.ToRowFilter().Should().BeNull();
    }

    [Fact]
    public void RoundTrip_IndexRangeFilter_PreservesStartAndEnd()
    {
        var original = new IndexRangeFilter(3, 10);
        var vm = new RowFilterEditorViewModel();

        vm.LoadFrom(original);

        vm.Mode.Should().Be(RowFilterEditorMode.IndexRange);
        vm.ToRowFilter().Should().Be(original);
    }

    [Fact]
    public void RoundTrip_PartitionBlockFilter_PreservesIndexAndCount()
    {
        var original = new PartitionBlockFilter(2, 5);
        var vm = new RowFilterEditorViewModel();

        vm.LoadFrom(original);

        vm.Mode.Should().Be(RowFilterEditorMode.PartitionBlock);
        vm.ToRowFilter().Should().Be(original);
    }

    /// <summary>
    ///     The failure class this whole ViewModel exists to prevent: entering Index-range values, switching to
    ///     Partition-block and back, must reproduce the original values — not defaults.
    /// </summary>
    [Fact]
    public void SwitchingModeAwayAndBack_PreservesPreviouslyEnteredIndexRangeValues()
    {
        var vm = new RowFilterEditorViewModel { Mode = RowFilterEditorMode.IndexRange, Start = 5, End = 42 };

        vm.Mode = RowFilterEditorMode.PartitionBlock;
        vm.PartitionIndex = 1;
        vm.PartitionCount = 3;
        vm.Mode = RowFilterEditorMode.IndexRange;

        vm.ToRowFilter().Should().Be(new IndexRangeFilter(5, 42));
    }

    /// <summary>Same guarantee in the other direction — Partition-block values survive a round trip through Index-range.</summary>
    [Fact]
    public void SwitchingModeAwayAndBack_PreservesPreviouslyEnteredPartitionBlockValues()
    {
        var vm = new RowFilterEditorViewModel { Mode = RowFilterEditorMode.PartitionBlock, PartitionIndex = 1, PartitionCount = 3 };

        vm.Mode = RowFilterEditorMode.IndexRange;
        vm.Start = 5;
        vm.End = 42;
        vm.Mode = RowFilterEditorMode.PartitionBlock;

        vm.ToRowFilter().Should().Be(new PartitionBlockFilter(1, 3));
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe.Tests
 * File: RowFilterTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Recipe.Models;
using Xunit;

namespace SlideGenerator.Recipe.Tests.Unit;

/// <summary>
///     Unit tests for the row-selection filters (<see cref="AllRowFilter" />, <see cref="IndexRangeFilter" />
///     and <see cref="PartitionBlockFilter" />), verifying that out-of-range counts and bounds select nothing
///     instead of throwing.
/// </summary>
public sealed class RowFilterTests
{
    /// <summary>
    ///     Verifies that <see cref="AllRowFilter" /> selects every data row and nothing when the data count is
    ///     zero or negative.
    /// </summary>
    /// <param name="dataCount">The total number of data rows handed to the filter.</param>
    /// <param name="expected">The expected 1-based data row indices.</param>
    [Theory]
    [InlineData(0, new int[] { })]
    [InlineData(-1, new int[] { })]
    [InlineData(3, new[] { 1, 2, 3 })]
    public void AllRowFilter_GetIndices_ReturnsAllRowsWithinDataCount(int dataCount, int[] expected)
    {
        new AllRowFilter().GetIndices(dataCount).Should().Equal(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="IndexRangeFilter" /> clamps both bounds to the available data rows and
    ///     selects nothing for a range outside the data, an inverted range, or an empty worksheet.
    /// </summary>
    /// <param name="start">The inclusive 1-based start row.</param>
    /// <param name="end">The inclusive 1-based end row.</param>
    /// <param name="dataCount">The total number of data rows handed to the filter.</param>
    /// <param name="expected">The expected 1-based data row indices.</param>
    [Theory]
    [InlineData(2, 4, 10, new[] { 2, 3, 4 })]
    [InlineData(0, 3, 10, new[] { 1, 2, 3 })]
    [InlineData(8, 20, 10, new[] { 8, 9, 10 })]
    [InlineData(12, 15, 10, new int[] { })]
    [InlineData(5, 2, 10, new int[] { })]
    [InlineData(1, 5, 0, new int[] { })]
    public void IndexRangeFilter_GetIndices_ClampsToDataCount(int start, int end, int dataCount, int[] expected)
    {
        new IndexRangeFilter(start, end).GetIndices(dataCount).Should().Equal(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="PartitionBlockFilter" /> selects exactly one block and selects nothing for
    ///     an invalid partition or an empty worksheet, without throwing.
    /// </summary>
    /// <param name="partitionIndex">The 0-based index of the block to select.</param>
    /// <param name="partitionCount">The total number of blocks.</param>
    /// <param name="dataCount">The total number of data rows handed to the filter.</param>
    /// <param name="expected">The expected 1-based data row indices.</param>
    [Theory]
    [InlineData(0, 2, 10, new[] { 1, 2, 3, 4, 5 })]
    [InlineData(1, 2, 10, new[] { 6, 7, 8, 9, 10 })]
    [InlineData(0, 0, 10, new int[] { })]
    [InlineData(2, 2, 10, new int[] { })]
    [InlineData(-1, 3, 10, new int[] { })]
    [InlineData(0, 3, 0, new int[] { })]
    public void PartitionBlockFilter_GetIndices_InvalidOrEmptySelection_ReturnsEmpty(
        int partitionIndex, int partitionCount, int dataCount, int[] expected)
    {
        new PartitionBlockFilter(partitionIndex, partitionCount).GetIndices(dataCount).Should().Equal(expected);
    }

    /// <summary>
    ///     Verifies the <see cref="PartitionBlockFilter.IsValid" /> truth table: a positive partition count with a
    ///     0-based index inside it is valid; everything else is not.
    /// </summary>
    /// <param name="partitionIndex">The 0-based index of the block.</param>
    /// <param name="partitionCount">The total number of blocks.</param>
    /// <param name="expected">Whether the block is valid.</param>
    [Theory]
    [InlineData(0, 2, true)]
    [InlineData(1, 2, true)]
    [InlineData(0, 3, true)]
    [InlineData(2, 2, false)]
    [InlineData(-1, 3, false)]
    [InlineData(0, 0, false)]
    [InlineData(0, -1, false)]
    public void PartitionBlockFilter_IsValid_ReflectsPartitionBounds(
        int partitionIndex, int partitionCount, bool expected)
    {
        new PartitionBlockFilter(partitionIndex, partitionCount).IsValid.Should().Be(expected);
    }
}

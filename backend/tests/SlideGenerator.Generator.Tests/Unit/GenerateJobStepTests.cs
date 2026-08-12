/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: GenerateJobStepTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Document.Abstractions.Sheet;
using SlideGenerator.Document.Models.Sheet;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Recipe.Domain.Models.Components;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for the internal helper methods of <see cref="GenerateJobStep" />.
/// </summary>
public sealed class GenerateJobStepTests
{
    #region RowFilter.GetIndices

    /// <summary>
    ///     Verifies that <see cref="AllRowFilter" /> yields all 1-based data row indices.
    ///     dataCount=4 → [1, 2, 3, 4].
    /// </summary>
    [Fact]
    public void GetIndices_AllRowFilter_ReturnsAllDataRows()
    {
        var result = new AllRowFilter().GetIndices(4).ToList();

        result.Should().Equal(1, 2, 3, 4);
    }

    /// <summary>
    ///     Verifies that <see cref="IndexRangeFilter" /> selects the correct 1-based data row range.
    ///     Start=2, End=4 → [2, 3, 4].
    /// </summary>
    [Fact]
    public void GetIndices_IndexRangeFilter_ReturnsCorrectRange()
    {
        var result = new IndexRangeFilter(2, 4).GetIndices(5).ToList();

        result.Should().Equal(2, 3, 4);
    }

    /// <summary>
    ///     Verifies that <see cref="PartitionBlockFilter" /> for block 0 of 2 returns the first half.
    ///     dataCount=6, PartitionBlock(0,2) → start=0, end=3 → [1, 2, 3].
    /// </summary>
    [Fact]
    public void GetIndices_PartitionBlockFilter_FirstBlock()
    {
        var result = new PartitionBlockFilter(0, 2).GetIndices(6).ToList();

        result.Should().Equal(1, 2, 3);
    }

    /// <summary>
    ///     Verifies that <see cref="PartitionBlockFilter" /> for block 1 of 2 returns the second half.
    ///     dataCount=6, PartitionBlock(1,2) → start=3, end=6 → [4, 5, 6].
    /// </summary>
    [Fact]
    public void GetIndices_PartitionBlockFilter_SecondBlock()
    {
        var result = new PartitionBlockFilter(1, 2).GetIndices(6).ToList();

        result.Should().Equal(4, 5, 6);
    }

    #endregion

    #region BuildHeaderToIndexMap

    /// <summary>
    ///     Verifies that the header map assigns a 0-based list index to each non-empty column name.
    /// </summary>
    [Fact]
    public void BuildHeaderMap_MapsColumnNameTo0BasedIndex()
    {
        var worksheet = Substitute.For<IReadOnlyWorksheet>();
        worksheet.GetRow(1).Returns(new List<string> { "A", "B", "C" });

        var result = Application.Utilities.BuildHeaderToIndexMap(worksheet);

        result.Should().BeEquivalentTo(new Dictionary<ColumnIdentifier, int>
        {
            [new ColumnIdentifier("A")] = 0,
            [new ColumnIdentifier("B")] = 1,
            [new ColumnIdentifier("C")] = 2
        });
    }

    #endregion

    #region BuildTextValues

    /// <summary>
    ///     Verifies that when the first column is empty and the second has a value, the second column value is used.
    /// </summary>
    [Fact]
    public void BuildTextValues_UsesFirstNonEmptyColumn()
    {
        var rowValues = new Dictionary<ColumnIdentifier, string> { [new ColumnIdentifier("ColA")] = "", [new ColumnIdentifier("ColB")] = "X" };
        var instructions = new List<TextInstruction>
        {
            new(
                new HashSet<string> { "Placeholder" },
                new List<ColumnIdentifier>
                {
                    new("ColA"),
                    new("ColB")
                })
        };

        var result = GenerateJobStep.BuildRowTextValues(rowValues, instructions);

        result.Should().ContainKey("Placeholder").WhoseValue.Should().Be("X");
    }

    /// <summary>
    ///     Verifies that when all columns are empty, the placeholder maps to an empty string.
    /// </summary>
    [Fact]
    public void BuildTextValues_UsesEmptyStringWhenNoColumnHasValue()
    {
        var rowValues = new Dictionary<ColumnIdentifier, string> { [new ColumnIdentifier("ColA")] = "", [new ColumnIdentifier("ColB")] = "" };
        var instructions = new List<TextInstruction>
        {
            new(
                new HashSet<string> { "Placeholder" },
                new List<ColumnIdentifier>
                {
                    new("ColA"),
                    new("ColB")
                })
        };

        var result = GenerateJobStep.BuildRowTextValues(rowValues, instructions);

        result.Should().ContainKey("Placeholder").WhoseValue.Should().Be("");
    }

    /// <summary>
    ///     Verifies that multiple <see cref="TextInstruction" /> items each produce their own placeholder entry.
    /// </summary>
    [Fact]
    public void BuildTextValues_MergesAllInstructions()
    {
        var rowValues = new Dictionary<ColumnIdentifier, string> { [new ColumnIdentifier("Name")] = "Alice", [new ColumnIdentifier("Role")] = "Manager" };
        var instructions = new List<TextInstruction>
        {
            new(new HashSet<string> { "Name" }, new List<ColumnIdentifier> { new("Name") }),
            new(new HashSet<string> { "Role" }, new List<ColumnIdentifier> { new("Role") })
        };

        var result = GenerateJobStep.BuildRowTextValues(rowValues, instructions);

        result.Should().ContainKey("Name").WhoseValue.Should().Be("Alice");
        result.Should().ContainKey("Role").WhoseValue.Should().Be("Manager");
    }

    #endregion
}

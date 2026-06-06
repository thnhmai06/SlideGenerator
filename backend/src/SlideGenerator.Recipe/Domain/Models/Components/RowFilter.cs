/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: RowFilter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.Json.Serialization;

namespace SlideGenerator.Recipe.Domain.Models.Components;

/// <summary>
///     Discriminator for the row-selection strategy applied to a worksheet.
/// </summary>
public enum RowFilterMode
{
    /// <summary>All rows participate.</summary>
    All,

    /// <summary>A contiguous range of rows by 1-based index.</summary>
    IndexRange,

    /// <summary>One block of an evenly divided partition.</summary>
    PartitionBlock
}

/// <summary>
///     Base type for row-filter configurations on a <see cref="Graphs.WorksheetNode" />.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(AllRowFilter), nameof(RowFilterMode.All))]
[JsonDerivedType(typeof(IndexRangeFilter), nameof(RowFilterMode.IndexRange))]
[JsonDerivedType(typeof(PartitionBlockFilter), nameof(RowFilterMode.PartitionBlock))]
public abstract record RowFilter
{
    /// <summary>Gets the strategy discriminator.</summary>
    public abstract RowFilterMode Mode { get; }
}

/// <summary>
///     Selects every row in the worksheet.
/// </summary>
public sealed record AllRowFilter : RowFilter
{
    /// <inheritdoc />
    public override RowFilterMode Mode => RowFilterMode.All;
}

/// <summary>
///     Selects a contiguous range of rows by 1-based index.
/// </summary>
/// <param name="From">Inclusive start row (1-based).</param>
/// <param name="To">Inclusive end row (1-based).</param>
public sealed record IndexRangeFilter(int From, int To) : RowFilter
{
    /// <inheritdoc />
    public override RowFilterMode Mode => RowFilterMode.IndexRange;
}

/// <summary>
///     Divides the worksheet rows into <see cref="PartitionCount" /> equal blocks and selects
///     the block at <see cref="PartitionIndex" /> (0-based).
/// </summary>
/// <remarks>
///     Block boundaries:
///     <code>
///         start = totalRows * partitionIndex / partitionCount
///         end   = totalRows * (partitionIndex + 1) / partitionCount
///     </code>
/// </remarks>
/// <param name="PartitionIndex">0-based index of the block to select.</param>
/// <param name="PartitionCount">Total number of equal-sized blocks.</param>
public sealed record PartitionBlockFilter(int PartitionIndex, int PartitionCount) : RowFilter
{
    /// <inheritdoc />
    public override RowFilterMode Mode => RowFilterMode.PartitionBlock;

    /// <summary>Resolves the inclusive start row (0-based) for the given total row count.</summary>
    public int ResolveStart(int totalRows)
    {
        return totalRows * PartitionIndex / PartitionCount;
    }

    /// <summary>Resolves the exclusive end row (0-based) for the given total row count.</summary>
    public int ResolveEnd(int totalRows)
    {
        return totalRows * (PartitionIndex + 1) / PartitionCount;
    }
}
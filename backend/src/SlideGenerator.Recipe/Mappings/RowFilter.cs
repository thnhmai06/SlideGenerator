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

namespace SlideGenerator.Recipe.Mappings;

/// <summary>
///     Discriminator for the row-selection strategy applied to a worksheet.
/// </summary>
public enum RowFilterMode : byte
{
    /// <summary>All rows participate.</summary>
    All,

    /// <summary>A contiguous range of rows by 1-based index.</summary>
    IndexRange,

    /// <summary>One block of an evenly divided partition.</summary>
    PartitionBlock
}

/// <summary>
///     Base type for row-filter configurations on a <see cref="WorksheetSource" />.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(AllRowFilter), nameof(RowFilterMode.All))]
[JsonDerivedType(typeof(IndexRangeFilter), nameof(RowFilterMode.IndexRange))]
[JsonDerivedType(typeof(PartitionBlockFilter), nameof(RowFilterMode.PartitionBlock))]
public abstract record RowFilter
{
    /// <summary>Gets the strategy discriminator — ignored by STJ since the <c>mode</c> discriminator already encodes this value.</summary>
    [JsonIgnore]
    public abstract RowFilterMode Mode { get; }

    /// <summary>
    ///     Returns the 1-based data row indices selected by this filter.
    ///     Index 1 is the first data row (absolute row 2 in the worksheet because row 1 is the header).
    ///     Callers are responsible for converting to absolute worksheet rows by adding 1.
    /// </summary>
    /// <param name="dataCount">Total number of data rows (= <c>RowCount − 1</c>; excludes the header).</param>
    /// <returns>Sequence of 1-based data row indices (≥ 1) in ascending order.</returns>
    public abstract IEnumerable<int> GetIndices(int dataCount);
}

/// <summary>
///     Selects every row in the worksheet.
/// </summary>
public sealed record AllRowFilter : RowFilter
{
    /// <inheritdoc />
    [JsonIgnore]
    public override RowFilterMode Mode => RowFilterMode.All;

    /// <inheritdoc />
    public override IEnumerable<int> GetIndices(int dataCount) => Enumerable.Range(1, dataCount);
}

/// <summary>
///     Selects a contiguous range of rows by 1-based index.
/// </summary>
/// <param name="Start">Inclusive start row (1-based).</param>
/// <param name="End">Inclusive end row (1-based).</param>
public sealed record IndexRangeFilter(int Start, int End) : RowFilter
{
    /// <inheritdoc />
    [JsonIgnore]
    public override RowFilterMode Mode => RowFilterMode.IndexRange;

    /// <inheritdoc />
    public override IEnumerable<int> GetIndices(int dataCount) => Enumerable.Range(Start, End - Start + 1);
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
    [JsonIgnore]
    public override RowFilterMode Mode => RowFilterMode.PartitionBlock;

    /// <summary>Resolves the inclusive start row (0-based) for the given total row count.</summary>
    public int GetStart(int totalRows) => totalRows * PartitionIndex / PartitionCount;

    /// <summary>Resolves the exclusive end row (0-based) for the given total row count.</summary>
    public int GetEnd(int totalRows) => totalRows * (PartitionIndex + 1) / PartitionCount;

    /// <inheritdoc />
    public override IEnumerable<int> GetIndices(int dataCount)
        => Enumerable.Range(GetStart(dataCount) + 1, GetEnd(dataCount) - GetStart(dataCount));
}

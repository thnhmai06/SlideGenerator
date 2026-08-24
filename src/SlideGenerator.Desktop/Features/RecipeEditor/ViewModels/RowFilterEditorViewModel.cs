/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RowFilterEditorViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>Which <see cref="RowFilter" /> subtype the radio group currently has selected.</summary>
public enum RowFilterEditorMode
{
    /// <summary>Maps to a <see langword="null" /> <see cref="RowFilter" /> — all rows (plan §5.2 "Tất cả").</summary>
    All,

    /// <summary>Maps to <see cref="IndexRangeFilter" /> (plan §5.2 "Khoảng dòng").</summary>
    IndexRange,

    /// <summary>Maps to <see cref="PartitionBlockFilter" /> (plan §5.2 "Chia khối").</summary>
    PartitionBlock
}

/// <summary>
///     Edits a <see cref="WorksheetSource.RowFilter" />. <see cref="Start" />/<see cref="End" /> and
///     <see cref="PartitionIndex" />/<see cref="PartitionCount" /> are kept as separate always-live fields
///     rather than being reset when <see cref="Mode" /> changes — switching the radio group away from Index
///     range and back must not lose what the user already typed, so nothing here clears on a mode switch;
///     <see cref="ToRowFilter" /> just projects whichever fields the current mode needs.
/// </summary>
public sealed partial class RowFilterEditorViewModel : ObservableObject
{
    [ObservableProperty] private RowFilterEditorMode _mode = RowFilterEditorMode.All;
    [ObservableProperty] private int _start = 1;
    [ObservableProperty] private int _end = 1;
    [ObservableProperty] private int _partitionIndex;
    [ObservableProperty] private int _partitionCount = 2;

    /// <summary>Populates the editor from an existing filter (or the "all rows" default when <see langword="null" />).</summary>
    public void LoadFrom(RowFilter? filter)
    {
        switch (filter)
        {
            case IndexRangeFilter r:
                Mode = RowFilterEditorMode.IndexRange;
                Start = r.Start;
                End = r.End;
                break;
            case PartitionBlockFilter p:
                Mode = RowFilterEditorMode.PartitionBlock;
                PartitionIndex = p.PartitionIndex;
                PartitionCount = p.PartitionCount;
                break;
            default:
                Mode = RowFilterEditorMode.All;
                break;
        }
    }

    /// <summary>
    ///     Projects the current mode's fields back into a <see cref="RowFilter" /> — <see langword="null" />
    ///     for All, matching how <c>JobsRepository</c> already treats <see langword="null" /> and
    ///     <see cref="AllRowFilter" /> as equivalent (never constructs the latter).
    /// </summary>
    public RowFilter? ToRowFilter()
    {
        return Mode switch
        {
            RowFilterEditorMode.IndexRange => new IndexRangeFilter(Start, End),
            RowFilterEditorMode.PartitionBlock => new PartitionBlockFilter(PartitionIndex, PartitionCount),
            _ => null
        };
    }
}

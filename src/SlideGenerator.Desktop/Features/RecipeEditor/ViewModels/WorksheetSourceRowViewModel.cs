/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: WorksheetSourceRowViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Workbooks;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     One <see cref="WorksheetSource" /> row: workbook/worksheet identity (fixed once added), a checkbox per
///     column from <see cref="WorksheetSummary" />'s preview headers, and a nested <see cref="RowFilterEditorViewModel" />.
///     <see cref="ToWorksheetSource" /> collapses "every box checked" back to <see langword="null" /> — the
///     same null-means-all fidelity <see cref="RowFilterEditorViewModel" /> already applies to <c>RowFilter</c>,
///     so re-saving an untouched source doesn't turn its forward-looking "all columns" into a column list
///     frozen at today's headers.
/// </summary>
public sealed partial class WorksheetSourceRowViewModel : ObservableObject
{
    /// <summary>Gets the workbook this source reads from.</summary>
    public WorkbookIdentifier Workbook { get; }

    /// <summary>Gets the worksheet within <see cref="Workbook" />.</summary>
    public WorksheetIdentifier Worksheet { get; }

    /// <summary>Gets the worksheet's data row count (excludes the header row).</summary>
    public int RowCount { get; }

    /// <summary>Gets the row-selection strategy editor for this source.</summary>
    public RowFilterEditorViewModel RowFilter { get; } = new();

    /// <summary>Gets one checkbox per column header, checked when the column is currently used.</summary>
    public ObservableCollection<ColumnCheckboxViewModel> Columns { get; } = [];

    /// <summary>Builds a row from a worksheet's structural summary and the source's saved configuration.</summary>
    public WorksheetSourceRowViewModel(WorksheetSummary summary, WorksheetSource source)
    {
        Workbook = source.Workbook;
        Worksheet = source.Worksheet;
        RowCount = summary.Count;

        var headers = summary.Preview?.Headers ?? [];
        foreach (var header in headers)
        {
            var used = source.UsedColumns is null || source.UsedColumns.Any(c => c.ColumnName == header);
            Columns.Add(new ColumnCheckboxViewModel(header, used));
        }

        RowFilter.LoadFrom(source.RowFilter);
    }

    /// <summary>Projects the current checkbox/row-filter state back into a <see cref="WorksheetSource" />.</summary>
    public WorksheetSource ToWorksheetSource()
    {
        var usedColumns = Columns.Count > 0 && Columns.All(c => c.IsChecked)
            ? null
            : Columns.Where(c => c.IsChecked).Select(c => new ColumnIdentifier(c.Name)).ToHashSet();
        return new WorksheetSource(Workbook, Worksheet, usedColumns, RowFilter.ToRowFilter());
    }
}

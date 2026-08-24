/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: WorksheetSourceRowViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Workbooks;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="WorksheetSourceRowViewModel" />'s round-trip fidelity — in particular that
///     "all columns checked" collapses back to <see langword="null" /> (all columns), matching how
///     <see cref="WorksheetSource.UsedColumns" /> already treats <see langword="null" /> as forward-looking
///     "every column, including future ones," not a frozen list of today's headers.
/// </summary>
public sealed class WorksheetSourceRowViewModelTests
{
    private static readonly WorkbookIdentifier Workbook = new("data.xlsx");
    private static readonly WorksheetIdentifier Worksheet = new("Sheet1");

    [Fact]
    public void Constructor_NullUsedColumns_AllCheckboxesStartChecked()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 10, new WorksheetPreview(["A", "B"], []));
        var source = new WorksheetSource(Workbook, Worksheet);

        var row = new WorksheetSourceRowViewModel(summary, source);

        row.Columns.Should().OnlyContain(c => c.IsChecked);
    }

    [Fact]
    public void ToWorksheetSource_AllCheckboxesChecked_CollapsesToNullUsedColumns()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 10, new WorksheetPreview(["A", "B"], []));
        var source = new WorksheetSource(Workbook, Worksheet, new HashSet<ColumnIdentifier> { new("A") });
        var row = new WorksheetSourceRowViewModel(summary, source);

        row.Columns.Single(c => c.Name == "B").IsChecked = true; // user checks the remaining column too

        row.ToWorksheetSource().UsedColumns.Should().BeNull();
    }

    [Fact]
    public void ToWorksheetSource_SomeCheckboxesUnchecked_ProjectsOnlyCheckedColumns()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 10, new WorksheetPreview(["A", "B"], []));
        var source = new WorksheetSource(Workbook, Worksheet);
        var row = new WorksheetSourceRowViewModel(summary, source);

        row.Columns.Single(c => c.Name == "B").IsChecked = false;

        row.ToWorksheetSource().UsedColumns.Should().BeEquivalentTo(new HashSet<ColumnIdentifier> { new("A") });
    }

    [Fact]
    public void RoundTrip_RowFilter_DelegatesToRowFilterEditorViewModel()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 10, new WorksheetPreview(["A"], []));
        var original = new IndexRangeFilter(2, 8);
        var source = new WorksheetSource(Workbook, Worksheet, RowFilter: original);

        var row = new WorksheetSourceRowViewModel(summary, source);

        row.ToWorksheetSource().RowFilter.Should().Be(original);
    }
}

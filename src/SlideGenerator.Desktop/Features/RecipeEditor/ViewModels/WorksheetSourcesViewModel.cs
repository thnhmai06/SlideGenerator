/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: WorksheetSourcesViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Owns one mapping's list of <see cref="WorksheetSource" /> rows (plan §5.2 "NGUỒN" panel). <see cref="Load" />
///     is pure — it takes already-fetched (source, summary) pairs, since summarizing a workbook is I/O that
///     belongs to <c>ISummaryCache</c>, not this ViewModel. <see cref="AddAsync" /> is the one place that does
///     I/O directly (file pick + summarize), since "add a source" only makes sense as a user-initiated action.
/// </summary>
public sealed partial class WorksheetSourcesViewModel(IFilePicker filePicker, ISummaryCache summaryCache) : ObservableObject
{
    /// <summary>Raised when a source is added or removed (not by <see cref="Load" />) — the coordinator marks
    ///     the recipe dirty on this.</summary>
    public event Action? Changed;

    /// <summary>Gets the current source rows.</summary>
    public ObservableCollection<WorksheetSourceRowViewModel> Sources { get; } = [];

    /// <summary>Replaces the current rows from already-fetched (source, summary) pairs.</summary>
    public void Load(IReadOnlyList<(WorksheetSource Source, SlideGenerator.Summarizer.Workbooks.WorksheetSummary Summary)> sources)
    {
        Sources.Clear();
        foreach (var (source, summary) in sources)
            Sources.Add(new WorksheetSourceRowViewModel(summary, source));
    }

    /// <summary>Projects every row back into the <see cref="WorksheetSource" /> list a <c>Mapping</c> needs.</summary>
    public IReadOnlyList<WorksheetSource> ToWorksheetSources()
    {
        return Sources.Select(s => s.ToWorksheetSource()).ToList();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var path = await filePicker.PickFileAsync("Thêm workbook",
            [new Avalonia.Platform.Storage.FilePickerFileType("Excel") { Patterns = ["*.xlsx", "*.xls"] }]).ConfigureAwait(true);
        if (path is null) return;

        var workbook = new WorkbookIdentifier(path);
        var summary = await summaryCache.GetWorkbookAsync(workbook).ConfigureAwait(true);
        var firstSheet = summary.Worksheets.FirstOrDefault();
        if (firstSheet is null) return; // empty workbook — nothing to add

        Sources.Add(new WorksheetSourceRowViewModel(firstSheet, new WorksheetSource(workbook, firstSheet.Worksheet)));
        Changed?.Invoke();
    }

    [RelayCommand]
    private void Remove(WorksheetSourceRowViewModel row)
    {
        Sources.Remove(row);
        Changed?.Invoke();
    }
}

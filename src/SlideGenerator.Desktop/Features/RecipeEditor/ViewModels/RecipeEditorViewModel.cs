/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipeEditorViewModel.cs
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
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Workbooks;
using RecipeModel = SlideGenerator.Recipe.Models.Recipe;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Coordinates the editor page for one <see cref="RecipeModel" />: owns the mapping navigator
///     (<see cref="Sessions" />) and the three per-mapping panels (<see cref="Canvas" />/<see cref="TextBindings" />/
///     <see cref="Sources" />), fetching each selected mapping's presentation/workbook summaries via
///     <see cref="ISummaryCache" /> and handing all three panels the same flattened <see cref="AvailableColumns" />
///     list — the same names must resolve to the same <c>BindingDisplayState</c> in the canvas and the text list.
///     Touched-placeholder/shape state lives on <see cref="MappingEditSession" />, not inside the panels
///     themselves, so switching mappings and back doesn't revert a confirmed Normalized binding to Suggested.
/// </summary>
public sealed partial class RecipeEditorViewModel : ObservableObject
{
    private readonly ISummaryCache _summaryCache;

    [ObservableProperty] private RecipeModel _recipe = new([]);
    [ObservableProperty] private MappingEditSession? _selectedSession;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private bool _isLoading;

    /// <summary>Gets one edit session per mapping in <see cref="Recipe" />, in order.</summary>
    public ObservableCollection<MappingEditSession> Sessions { get; } = [];

    /// <summary>Gets whether the mapping navigator should be shown — only meaningful once there's more than one mapping to pick between.</summary>
    public bool ShowMappingNavigator => Sessions.Count >= 2;

    /// <summary>Gets every column visible to <see cref="SelectedSession" />'s worksheet sources — fed identically to <see cref="Canvas" /> and <see cref="TextBindings" />.</summary>
    public IReadOnlyList<string> AvailableColumns { get; private set; } = [];

    /// <summary>Gets the canvas panel for the currently selected mapping's template slide.</summary>
    public SlideCanvasViewModel Canvas { get; }

    /// <summary>Gets the text-placeholder binding panel for the currently selected mapping.</summary>
    public TextBindingsViewModel TextBindings { get; }

    /// <summary>Gets the worksheet-source panel for the currently selected mapping.</summary>
    public WorksheetSourcesViewModel Sources { get; }

    /// <summary>Constructs the editor, wiring the three child panels it coordinates.</summary>
    public RecipeEditorViewModel(ISummaryCache summaryCache, IFilePicker filePicker)
    {
        _summaryCache = summaryCache;
        Canvas = new SlideCanvasViewModel();
        TextBindings = new TextBindingsViewModel();
        Sources = new WorksheetSourcesViewModel(filePicker, summaryCache);
    }

    /// <summary>Loads the given recipe into the editor, selecting its first mapping (if any), and clears the dirty flag.</summary>
    public async Task InitializeAsync(RecipeModel recipe)
    {
        Recipe = recipe;
        IsDirty = false;

        Sessions.Clear();
        foreach (var mapping in recipe.Mappings) Sessions.Add(new MappingEditSession(mapping));
        OnPropertyChanged(nameof(ShowMappingNavigator));

        SelectedSession = null;
        if (Sessions.Count > 0) await SelectSessionAsync(Sessions[0]).ConfigureAwait(true);
    }

    /// <summary>Commits in-flight edits from the previously selected session, then loads <paramref name="session" />'s mapping into the three panels.</summary>
    [RelayCommand]
    public async Task SelectSessionAsync(MappingEditSession session)
    {
        ProjectCurrentSessionEdits();
        SelectedSession = session;

        IsLoading = true;
        try
        {
            await LoadMappingAsync(session).ConfigureAwait(true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Projects the three panels' current edits back into <see cref="SelectedSession" />'s mapping — call before navigating away so edits aren't lost.</summary>
    public void ProjectCurrentSessionEdits()
    {
        if (SelectedSession is null) return;

        SelectedSession.Mapping = SelectedSession.Mapping with
        {
            TextInstructions = TextBindings.ToTextInstructions(),
            ImageInstructions = Canvas.ToImageInstructions(),
            Sources = Sources.ToWorksheetSources()
        };
        IsDirty = true;
    }

    private async Task LoadMappingAsync(MappingEditSession session)
    {
        var mapping = session.Mapping;

        var presentationSummary = await _summaryCache.GetPresentationAsync(mapping.Template.Presentation).ConfigureAwait(true);
        var slide = presentationSummary.Slides.FirstOrDefault(s => s.Slide == mapping.Template.Slide);
        if (slide is null) return; // template slide no longer exists in the presentation — nothing to show

        var worksheetPairs = new List<(WorksheetSource Source, WorksheetSummary Summary)>();
        foreach (var source in mapping.Sources)
        {
            var workbookSummary = await _summaryCache.GetWorkbookAsync(source.Workbook).ConfigureAwait(true);
            var worksheetSummary = workbookSummary.Worksheets.FirstOrDefault(w => w.Worksheet == source.Worksheet);
            if (worksheetSummary is not null) worksheetPairs.Add((source, worksheetSummary));
        }

        AvailableColumns = FlattenAvailableColumns(worksheetPairs.Select(p => p.Summary).ToList());
        OnPropertyChanged(nameof(AvailableColumns));

        Canvas.Load(slide, mapping.ImageInstructions, AvailableColumns, session.TouchedShapes);
        TextBindings.Load(slide.Placeholders, mapping.TextInstructions, AvailableColumns, session.TouchedPlaceholders);
        Sources.Load(worksheetPairs);
    }

    /// <summary>Unions and dedupes every worksheet's preview headers — the same column list every panel must see for a mapping's suggestions/dropdowns to agree.</summary>
    internal static IReadOnlyList<string> FlattenAvailableColumns(IReadOnlyList<WorksheetSummary> summaries)
    {
        return summaries
            .SelectMany(s => s.Preview?.Headers ?? [])
            .Distinct()
            .ToList();
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipesViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Generator;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Settings.Immutable;
using RecipeModel = SlideGenerator.Recipe.Models.Recipe;

namespace SlideGenerator.Desktop.Features.Recipes.ViewModels;

/// <summary>
///     Owns the Recipes list, search, import/new entry points, and the per-recipe "recent runs" panel.
///     Editing (<see cref="IsEditorOpen" />) is a stand-in placeholder until P4 builds the real editor — the
///     command surface (<see cref="NewCommand" />, each row's <c>EditCommand</c>) is final and won't need to
///     change when P4 lands; only what's shown while <see cref="IsEditorOpen" /> is true will.
/// </summary>
public sealed partial class RecipesViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IFilePicker _filePicker;
    private readonly IRecipePackageService _packageService;
    private readonly IRecipeRepository _repository;
    private readonly IService _service;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISummaryCache _summaryCache;
    private readonly List<RecipeListItemViewModel> _all = [];

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private RecipeListItemViewModel? _selectedRecipe;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private bool _isLoadingRecentRuns;
    [ObservableProperty] private RecipeEditorViewModel? _editor;

    // Detail-pane stat chips (plan §5.2: "N mapping · N nguồn · N text · N ảnh · ~N records") — fetched only
    // when a recipe is selected, same "don't pay for data a list view doesn't need" precedent as recent runs.
    [ObservableProperty] private bool _isLoadingStats;
    [ObservableProperty] private int _mappingCount;
    [ObservableProperty] private int _sourceCount;
    [ObservableProperty] private int _textInstructionCount;
    [ObservableProperty] private int _imageInstructionCount;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(RecordCountDisplay))] private int? _recordCount;

    /// <summary>Gets the record-count chip text — "~N" once counted, or an em dash if counting failed
    ///     (plan §5.2: "lỗi → '—'").</summary>
    public string RecordCountDisplay => RecordCount is { } n ? $"~{n}" : "—";

    /// <summary>Gets the recipes matching <see cref="SearchText" />, most recently updated first.</summary>
    public ObservableCollection<RecipeListItemViewModel> FilteredRecipes { get; } = [];

    /// <summary>Gets up to 5 most recent runs of <see cref="SelectedRecipe" />, newest first.</summary>
    public ObservableCollection<RecentRunViewModel> RecentRuns { get; } = [];

    /// <summary>Gets whether <see cref="FilteredRecipes" /> has at least one entry — drives the empty state.</summary>
    public bool HasResults => FilteredRecipes.Count > 0;

    /// <summary>Gets whether <see cref="RecentRuns" /> has at least one entry.</summary>
    public bool HasRecentRuns => RecentRuns.Count > 0;

    /// <summary>Raised when a run was just started from this page — the Shell navigates to Runs on this.</summary>
    public event Action<string>? RunStarted;

    /// <summary>Raised when Ctrl+F is pressed — the view focuses the search box (Ctrl+F has no meaningful
    ///     ViewModel-side effect on its own, this only exists so the shortcut is bindable via
    ///     <c>UserControl.KeyBindings</c> like every other keyboard shortcut in this view).</summary>
    public event Action? FocusSearchRequested;

    /// <summary>Constructs the ViewModel and starts the initial load.</summary>
    public RecipesViewModel(IRecipeRepository repository, IRecipePackageService packageService, IService service,
        IDialogService dialogService, IFilePicker filePicker, IServiceProvider serviceProvider, ISummaryCache summaryCache)
    {
        _repository = repository;
        _packageService = packageService;
        _service = service;
        _dialogService = dialogService;
        _filePicker = filePicker;
        _serviceProvider = serviceProvider;
        _summaryCache = summaryCache;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var metadata = await _repository.ListAsync().ConfigureAwait(true);
            foreach (var item in _all) Unsubscribe(item);
            _all.Clear();
            _all.AddRange(metadata.Select(CreateItem));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private RecipeListItemViewModel CreateItem(IRecipeMetadata metadata)
    {
        var item = new RecipeListItemViewModel(metadata, _repository, _packageService, _dialogService, _filePicker);
        item.EditRequested += OnEditRequested;
        item.RunStarted += OnRunStarted;
        item.Deleted += OnDeleted;
        item.Duplicated += OnDuplicated;
        return item;
    }

    private static void Unsubscribe(RecipeListItemViewModel item)
    {
        // No-op placeholder for symmetry with CreateItem — C# events don't need explicit -= before an
        // instance is dropped from every collection that held it (no other subscriber keeps it alive).
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedRecipeChanged(RecipeListItemViewModel? value)
    {
        RecentRuns.Clear();
        OnPropertyChanged(nameof(HasRecentRuns));
        MappingCount = 0;
        SourceCount = 0;
        TextInstructionCount = 0;
        ImageInstructionCount = 0;
        RecordCount = null;
        if (value is null) return;
        _ = LoadRecentRunsAsync(value.Id);
        _ = LoadStatsAsync(value.Id);
    }

    private async Task LoadStatsAsync(int recipeId)
    {
        IsLoadingStats = true;
        try
        {
            var entry = await _repository.GetAsync(recipeId).ConfigureAwait(true);
            var mappings = entry.Recipe.Mappings;
            MappingCount = mappings.Count;
            SourceCount = mappings.Sum(m => m.Sources.Count);
            TextInstructionCount = mappings.Sum(m => m.TextInstructions.Count);
            ImageInstructionCount = mappings.Sum(m => m.ImageInstructions.Count);

            var total = 0;
            foreach (var source in mappings.SelectMany(m => m.Sources))
            {
                var workbook = await _summaryCache.GetWorkbookAsync(source.Workbook, false).ConfigureAwait(true);
                var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Worksheet == source.Worksheet);
                if (worksheet is not null) total += worksheet.Count;
            }

            RecordCount = total;
        }
        catch (Exception)
        {
            RecordCount = null; // shown as "—" — a failed count must not block viewing the recipe
        }
        finally
        {
            IsLoadingStats = false;
        }
    }

    private void ApplyFilter()
    {
        var previouslySelectedId = SelectedRecipe?.Id;
        var matching = _all
            .Where(r => string.IsNullOrWhiteSpace(SearchText) ||
                        r.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.UpdatedTimestamp)
            .ToList();

        FilteredRecipes.Clear();
        foreach (var recipe in matching) FilteredRecipes.Add(recipe);
        OnPropertyChanged(nameof(HasResults));

        SelectedRecipe = previouslySelectedId is not null
            ? FilteredRecipes.FirstOrDefault(r => r.Id == previouslySelectedId)
            : null;
    }

    private async Task LoadRecentRunsAsync(int recipeId)
    {
        IsLoadingRecentRuns = true;
        try
        {
            var active = await _service.ListActiveAsync(includeLogs: false).ConfigureAwait(true);
            var completed = await _service.ListCompletedAsync(includeLogs: false).ConfigureAwait(true);
            var runs = active.Concat(completed)
                .Where(kv => kv.Value.Request.RecipeId == recipeId)
                .OrderByDescending(kv => kv.Value.CreatedAt)
                .Take(5)
                .Select(kv => new RecentRunViewModel(kv.Key, kv.Value.Request.Name, kv.Value.JobStatus, kv.Value.CreatedAt));

            RecentRuns.Clear();
            foreach (var run in runs) RecentRuns.Add(run);
            OnPropertyChanged(nameof(HasRecentRuns));
        }
        finally
        {
            IsLoadingRecentRuns = false;
        }
    }

    private void OnEditRequested(RecipeListItemViewModel item)
    {
        IsEditorOpen = true;
        _ = OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(int recipeId)
    {
        var entry = await _repository.GetAsync(recipeId).ConfigureAwait(true);
        var editor = _serviceProvider.GetRequiredService<RecipeEditorViewModel>();
        editor.Saved += OnEditorSaved;
        editor.RunStarted += OnRunStarted;
        await editor.InitializeAsync(entry.Id, entry.Name, entry.Recipe).ConfigureAwait(true);
        Editor = editor;
    }

    private void OnRunStarted(string requestId)
    {
        // No-op when the run started from a list row (no editor open) — closes the editor when it started
        // from Guided step ④'s "Lưu và chạy" instead, since the Shell is about to navigate to Runs anyway.
        IsEditorOpen = false;
        Editor = null;
        RunStarted?.Invoke(requestId);
    }

    private void OnDeleted(RecipeListItemViewModel item)
    {
        _all.Remove(item);
        ApplyFilter();
    }

    private void OnDuplicated(IRecipeMetadata metadata)
    {
        _all.Add(CreateItem(metadata));
        ApplyFilter();
    }

    [RelayCommand]
    private void FocusSearch()
    {
        FocusSearchRequested?.Invoke();
    }

    [RelayCommand]
    private void New()
    {
        IsEditorOpen = true;
        _ = OpenNewEditorAsync();
    }

    private async Task OpenNewEditorAsync()
    {
        var editor = _serviceProvider.GetRequiredService<RecipeEditorViewModel>();
        editor.Saved += OnEditorSaved;
        editor.RunStarted += OnRunStarted;
        await editor.InitializeAsync(new RecipeModel([])).ConfigureAwait(true);
        Editor = editor;
    }

    private void OnEditorSaved()
    {
        _ = LoadAsync();
    }

    /// <summary>Closes the editor — asks for confirmation first if there are unsaved edits (plan §5.2: "Rời
    ///     editor lúc dirty → dialog xác nhận").</summary>
    [RelayCommand]
    private async Task CloseEditorAsync()
    {
        if (Editor is { IsDirty: true })
        {
            var confirmed = await _dialogService.ConfirmAsync(
                LocalizationService.Instance["recipes.unsavedChanges.title"],
                LocalizationService.Instance["recipes.unsavedChanges.message"],
                LocalizationService.Instance["recipes.unsavedChanges.leave"],
                LocalizationService.Instance["recipes.unsavedChanges.stay"]).ConfigureAwait(true);
            if (!confirmed) return;
        }

        IsEditorOpen = false;
        Editor = null;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = await _filePicker
            .PickFileAsync(LocalizationService.Instance["recipes.import.dialogTitle"], [new FilePickerFileType("Recipe") { Patterns = [$"*{RecipePackageFormat.PackageExtension}"] }])
            .ConfigureAwait(true);
        if (path is null) return;

        try
        {
            var metadata = await _packageService
                .ImportAsync(path, (NameAndPaths.ImportedFolder.WorkbooksPath, NameAndPaths.ImportedFolder.PresentationsPath))
                .ConfigureAwait(true);
            _all.Add(CreateItem(metadata));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

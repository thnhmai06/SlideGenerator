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
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
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
    private readonly List<RecipeListItemViewModel> _all = [];

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private RecipeListItemViewModel? _selectedRecipe;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isEditorOpen;
    [ObservableProperty] private string _editorTitle = "";
    [ObservableProperty] private bool _isLoadingRecentRuns;
    [ObservableProperty] private RecipeEditorViewModel? _editor;

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

    /// <summary>Constructs the ViewModel and starts the initial load.</summary>
    public RecipesViewModel(IRecipeRepository repository, IRecipePackageService packageService, IService service,
        IDialogService dialogService, IFilePicker filePicker, IServiceProvider serviceProvider)
    {
        _repository = repository;
        _packageService = packageService;
        _service = service;
        _dialogService = dialogService;
        _filePicker = filePicker;
        _serviceProvider = serviceProvider;
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
        if (value is not null) _ = LoadRecentRunsAsync(value.Id);
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
        EditorTitle = item.Name;
        IsEditorOpen = true;
        _ = OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(int recipeId)
    {
        var entry = await _repository.GetAsync(recipeId).ConfigureAwait(true);
        var editor = _serviceProvider.GetRequiredService<RecipeEditorViewModel>();
        await editor.InitializeAsync(entry.Recipe).ConfigureAwait(true);
        Editor = editor;
    }

    private void OnRunStarted(string requestId)
    {
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
    private void New()
    {
        EditorTitle = "Recipe mới";
        IsEditorOpen = true;
        _ = OpenNewEditorAsync();
    }

    private async Task OpenNewEditorAsync()
    {
        var editor = _serviceProvider.GetRequiredService<RecipeEditorViewModel>();
        await editor.InitializeAsync(new RecipeModel([])).ConfigureAwait(true);
        Editor = editor;
    }

    [RelayCommand]
    private void CloseEditor()
    {
        IsEditorOpen = false;
        Editor = null;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = await _filePicker
            .PickFileAsync("Nhập recipe", [new FilePickerFileType("Recipe") { Patterns = [$"*{RecipePackageFormat.PackageExtension}"] }])
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

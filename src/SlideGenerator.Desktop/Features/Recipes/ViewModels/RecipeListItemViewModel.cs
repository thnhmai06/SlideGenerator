/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipeListItemViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Services;

namespace SlideGenerator.Desktop.Features.Recipes.ViewModels;

/// <summary>
///     One recipe row. Only carries what <c>IRecipeRepository.ListAsync</c> actually returns
///     (<see cref="IRecipeMetadata" /> — id, name, timestamps) — mapping/source counts are not part of that
///     lightweight projection (they live on the full <c>RecipeEntry.Recipe</c>, fetched only via
///     <c>GetAsync</c>), so showing them here would mean an N+1 fetch per row for a list view. Matches the
///     same "don't pay for data a list view doesn't need" precedent as <c>includeLogs</c> in Runs.
/// </summary>
public sealed partial class RecipeListItemViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IFilePicker _filePicker;
    private readonly IRecipePackageService _packageService;
    private readonly IRecipeRepository _repository;

    /// <summary>Gets the database id.</summary>
    public int Id { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private DateTimeOffset _updatedTimestamp;

    /// <summary>Raised when the user chooses to edit this recipe.</summary>
    public event Action<RecipeListItemViewModel>? EditRequested;

    /// <summary>Raised when the user chooses to run this recipe — carries the new request id once started.</summary>
    public event Action<string>? RunStarted;

    /// <summary>Raised after this recipe has been deleted, so the parent list can remove the row.</summary>
    public event Action<RecipeListItemViewModel>? Deleted;

    /// <summary>Raised after a duplicate of this recipe has been created, so the parent list can add the row.</summary>
    public event Action<IRecipeMetadata>? Duplicated;

    /// <summary>Constructs a row from a repository listing entry.</summary>
    public RecipeListItemViewModel(IRecipeMetadata metadata, IRecipeRepository repository,
        IRecipePackageService packageService, IDialogService dialogService, IFilePicker filePicker)
    {
        Id = metadata.Id;
        _name = metadata.Name;
        _updatedTimestamp = metadata.UpdatedTimestamp;
        _repository = repository;
        _packageService = packageService;
        _dialogService = dialogService;
        _filePicker = filePicker;
    }

    [RelayCommand]
    private void Edit()
    {
        EditRequested?.Invoke(this);
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        var requestId = await _dialogService.ShowRunDialogAsync(Id, Name).ConfigureAwait(true);
        if (requestId is not null) RunStarted?.Invoke(requestId);
    }

    [RelayCommand]
    private async Task DuplicateAsync()
    {
        var entry = await _repository.GetAsync(Id).ConfigureAwait(true);
        var copy = await _repository.AddAsync(new RecipeInput($"{Name}{LocalizationService.Instance["recipes.duplicateSuffix"]}", entry.Recipe)).ConfigureAwait(true);
        Duplicated?.Invoke(copy);
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = await _filePicker
            .PickSaveFileAsync(LocalizationService.Instance["recipes.export.dialogTitle"], $"{Name}{RecipePackageFormat.PackageExtension}")
            .ConfigureAwait(true);
        if (path is not null) await _packageService.ExportAsync(Id, path).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirmed = await _dialogService
            .ConfirmAsync(LocalizationService.Instance["recipes.delete.title"],
                string.Format(LocalizationService.Instance["recipes.delete.message"], Name),
                LocalizationService.Instance["recipes.delete.confirm"], LocalizationService.Instance["recipes.delete.cancel"])
            .ConfigureAwait(true);
        if (!confirmed) return;

        await _repository.DeleteAsync(Id).ConfigureAwait(true);
        Deleted?.Invoke(this);
    }
}

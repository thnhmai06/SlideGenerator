/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RunDialogViewModel.cs
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
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Generator;

namespace SlideGenerator.Desktop.Features.Recipes.ViewModels;

/// <summary>
///     Backs the run confirmation dialog. Every field change re-previews via <c>IService.PreviewAsync</c> —
///     the same conflict definition <c>IService.CreateAsync</c> enforces at submit time (see the plan's
///     Run dialog contract) — so the user sees every output-path conflict and the full job fan-out before
///     committing, never a frontend-invented approximation of it.
/// </summary>
public sealed partial class RunDialogViewModel : ObservableObject
{
    private readonly IFilePicker _filePicker;
    private readonly IService _service;
    private int _recipeId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _name = "";

    [ObservableProperty] private PresentationType _outputType = PresentationType.Pptx;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _saveFolder = "";

    [ObservableProperty] private bool _allowLocalPaths;
    [ObservableProperty] private bool _showAdvanced;
    [ObservableProperty] private bool _isPreviewLoading;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>Gets every job this run would create, with its output path and any conflict.</summary>
    public ObservableCollection<PlannedJob> PlannedJobs { get; } = [];

    /// <summary>Gets the request id created by <see cref="StartCommand" />, once it has run successfully.</summary>
    public string? CreatedRequestId { get; private set; }

    /// <summary>Raised when the dialog should close — <see langword="true" /> if a request was created.</summary>
    public event Action<bool>? RequestClose;

    /// <summary>Constructs the ViewModel; call <see cref="Initialize" /> before showing the dialog.</summary>
    public RunDialogViewModel(IService service, IFilePicker filePicker)
    {
        _service = service;
        _filePicker = filePicker;
    }

    /// <summary>Sets the recipe this dialog runs and seeds a default name/preview.</summary>
    public void Initialize(int recipeId, string recipeName)
    {
        _recipeId = recipeId;
        Name = $"{recipeName} {DateTime.Now:yyyy-MM-dd HH-mm}";
        _ = RefreshPreviewAsync();
    }

    /// <summary>Gets whether any planned job's output path conflicts with another job.</summary>
    public bool HasConflicts => PlannedJobs.Any(j => j.ConflictKind != ConflictKind.None);

    partial void OnOutputTypeChanged(PresentationType value)
    {
        _ = RefreshPreviewAsync();
    }

    partial void OnSaveFolderChanged(string value)
    {
        _ = RefreshPreviewAsync();
    }

    private async Task RefreshPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(SaveFolder) || string.IsNullOrWhiteSpace(Name))
        {
            PlannedJobs.Clear();
            StartCommand.NotifyCanExecuteChanged();
            return;
        }

        IsPreviewLoading = true;
        ErrorMessage = null;
        try
        {
            var request = BuildRequest();
            var planned = await _service.PreviewAsync(request).ConfigureAwait(true);
            PlannedJobs.Clear();
            foreach (var job in planned) PlannedJobs.Add(job);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            PlannedJobs.Clear();
        }
        finally
        {
            IsPreviewLoading = false;
            OnPropertyChanged(nameof(HasConflicts));
            StartCommand.NotifyCanExecuteChanged();
        }
    }

    private Request BuildRequest()
    {
        return new Request(_recipeId, Name, OutputType, SaveFolder, AllowLocalPaths);
    }

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var folder = await _filePicker.PickFolderAsync(LocalizationService.Instance["runDialog.pickFolderDialogTitle"]).ConfigureAwait(true);
        if (folder is not null) SaveFolder = folder;
    }

    private bool CanStart()
    {
        return !IsPreviewLoading && PlannedJobs.Count > 0 && !HasConflicts &&
               !string.IsNullOrWhiteSpace(SaveFolder) && !string.IsNullOrWhiteSpace(Name);
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        try
        {
            CreatedRequestId = await _service.CreateAsync(BuildRequest()).ConfigureAwait(true);
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}

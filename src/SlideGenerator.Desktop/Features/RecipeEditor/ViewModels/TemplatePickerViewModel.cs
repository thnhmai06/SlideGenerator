/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TemplatePickerViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Backs the "add mapping" template picker dialog: pick a presentation file, then pick one of its slides
///     as the new mapping's template. The one piece a brand-new mapping genuinely can't exist without — every
///     other P4.4 mapping-list operation (remove/reorder) only touches <c>Sessions</c>, already in place.
/// </summary>
public sealed partial class TemplatePickerViewModel : ObservableObject
{
    private readonly IFilePicker _filePicker;
    private readonly ISummaryCache _summaryCache;
    private PresentationIdentifier? _presentationId;

    [ObservableProperty] private string? _presentationPath;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private TemplateSlideRow? _selectedSlide;

    /// <summary>Gets the slides of the currently picked presentation.</summary>
    public ObservableCollection<TemplateSlideRow> Slides { get; } = [];

    /// <summary>Gets the picked template, set right before <see cref="RequestClose" /> fires with <see langword="true" />.</summary>
    public PresentationSource? Result { get; private set; }

    /// <summary>Raised when the dialog should close — <see langword="true" /> if a template was picked.</summary>
    public event Action<bool>? RequestClose;

    /// <summary>Constructs the ViewModel.</summary>
    public TemplatePickerViewModel(IFilePicker filePicker, ISummaryCache summaryCache)
    {
        _filePicker = filePicker;
        _summaryCache = summaryCache;
    }

    [RelayCommand]
    private async Task PickPresentationAsync()
    {
        var path = await _filePicker.PickFileAsync("Chọn presentation mẫu",
            [new Avalonia.Platform.Storage.FilePickerFileType("PowerPoint") { Patterns = ["*.pptx", "*.potx", "*.ppsx"] }]).ConfigureAwait(true);
        if (path is null) return;

        PresentationPath = path;
        _presentationId = new PresentationIdentifier(path);
        SelectedSlide = null;
        ErrorMessage = null;
        foreach (var row in Slides) row.Preview?.Dispose();
        Slides.Clear();

        IsLoading = true;
        try
        {
            var summary = await _summaryCache.GetPresentationAsync(_presentationId).ConfigureAwait(true);
            foreach (var slide in summary.Slides)
            {
                var preview = slide.Preview is { Length: > 0 } bytes ? new Bitmap(new MemoryStream(bytes)) : null;
                Slides.Add(new TemplateSlideRow(slide, preview));
            }
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

    private bool CanConfirm()
    {
        return _presentationId is not null && SelectedSlide is not null;
    }

    partial void OnSelectedSlideChanged(TemplateSlideRow? value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        Result = new PresentationSource(_presentationId!, SelectedSlide!.Slide.Slide);
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}

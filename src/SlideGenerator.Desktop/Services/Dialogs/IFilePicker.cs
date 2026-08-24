/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: IFilePicker.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SlideGenerator.Desktop.Services.Dialogs;

/// <summary>
///     Thin wrapper over <see cref="IStorageProvider" /> so ViewModels can pick files/folders without a
///     direct reference to any <see cref="Window" />.
/// </summary>
public interface IFilePicker
{
    /// <summary>Opens a single-file picker. Returns the absolute path, or <see langword="null" /> if canceled.</summary>
    Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypes = null);

    /// <summary>Opens a folder picker. Returns the absolute path, or <see langword="null" /> if canceled.</summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>Opens a save-file picker. Returns the absolute path, or <see langword="null" /> if canceled.</summary>
    Task<string?> PickSaveFileAsync(string title, string? suggestedFileName = null,
        IReadOnlyList<FilePickerFileType>? fileTypes = null);
}

/// <inheritdoc cref="IFilePicker" />
public sealed class FilePicker : IFilePicker
{
    private static IStorageProvider StorageProvider =>
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!)
        .MainWindow!.StorageProvider;

    /// <inheritdoc />
    public async Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        }).ConfigureAwait(false);
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        }).ConfigureAwait(false);
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> PickSaveFileAsync(string title, string? suggestedFileName = null,
        IReadOnlyList<FilePickerFileType>? fileTypes = null)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = fileTypes
        }).ConfigureAwait(false);
        return file?.TryGetLocalPath();
    }
}

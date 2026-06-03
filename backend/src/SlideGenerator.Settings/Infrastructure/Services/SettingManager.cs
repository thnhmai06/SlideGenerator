/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: SettingManager.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Rules;

namespace SlideGenerator.Settings.Infrastructure.Services;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
/// </summary>
/// <param name="serializer">The serializer used to persist settings to disk.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class SettingManager(
    ISerializer serializer,
    ILogger<SettingManager>? logger = null)
    : ISettingManager
{
    /// <summary>
    ///     Gets the full file path where settings are stored.
    /// </summary>
    private string FilePath => NameAndPaths.SettingsFile.GetFilePath(serializer.FileExtension);

    /// <inheritdoc />
    public Setting Current { get; private set; } = new();

    /// <summary>
    ///     Asynchronously loads settings from the disk.
    /// </summary>
    /// <returns>True if the settings were successfully loaded; false if the file does not exist.</returns>
    public async Task<bool> Load()
    {
        if (!File.Exists(FilePath))
        {
            logger?.LogInformation("Setting file not found at {Path}. Using default settings.", L(FilePath));
            return false;
        }

        try
        {
            logger?.LogDebug("Loading settings from {Path}", L(FilePath));
            var source = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            Current = serializer.Deserialize<Setting>(source);
            logger?.LogInformation("Successfully loaded settings from {Path}", L(FilePath));
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error loading settings from {Path}. Resetting to defaults.", L(FilePath));
            await ResetToDefaults().ConfigureAwait(false);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Asynchronously saves the current settings to disk.
    /// </summary>
    /// <returns>True if the operation completed successfully.</returns>
    public async Task Save()
    {
        try
        {
            logger?.LogDebug("Saving settings to {Path}", L(FilePath));
            var folderName = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folderName)) Directory.CreateDirectory(folderName);

            var content = serializer.Serialize(Current);
            await File.WriteAllTextAsync(FilePath, content).ConfigureAwait(false);
            logger?.LogInformation("Successfully saved settings to {Path}", L(FilePath));
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error saving settings to {Path}", L(FilePath));
            throw;
        }
    }

    /// <summary>
    ///     Resets the settings to their default values and persists them to disk.
    /// </summary>
    /// <returns>A task representing the reset and save operation.</returns>
    public async Task ResetToDefaults()
    {
        var defaultSetting = new Setting();
        await Update(defaultSetting).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the current settings state and persists it to disk.
    /// </summary>
    /// <param name="newSetting">The new settings object to apply.</param>
    /// <returns>A task representing the save operation.</returns>
    public async Task Update(Setting newSetting)
    {
        Current = newSetting;
        await Save().ConfigureAwait(false);
    }

    private static string L(string? s)
    {
        return s?.ReplaceLineEndings(" ") ?? "";
    }
}

/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: SettingManager.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Models;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Settings.Services;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
/// </summary>
/// <param name="serializer">The serializer used to persist settings to disk.</param>
/// <param name="logger">The logger instance.</param>
public sealed class SettingManager(Serializer serializer, ILogger<SettingManager> logger) : ISettingProvider
{
    /// <summary>
    ///     Gets the full file path where settings are stored.
    /// </summary>
    private string FilePath => NameAndPathRules.Setting.GetFilePath(serializer.FileExtension);

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
            logger.LogInformation("Settings file not found at {Path}. Using default settings.", FilePath);
            return false;
        }

        try
        {
            logger.LogDebug("Loading settings from {Path}", FilePath);
            var source = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            var loaded = serializer.Deserialize<Setting>(source);
            Current = loaded;
            logger.LogInformation("Successfully loaded settings from {Path}", FilePath);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error loading settings from {Path}. Resetting to defaults.", FilePath);
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
            logger.LogDebug("Saving settings to {Path}", FilePath);
            var folderName = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folderName)) Directory.CreateDirectory(folderName);

            var content = serializer.Serialize(Current);
            await File.WriteAllTextAsync(FilePath, content).ConfigureAwait(false);
            logger.LogInformation("Successfully saved settings to {Path}", FilePath);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving settings to {Path}", FilePath);
            throw;
        }
    }

    /// <summary>
    ///     Resets the settings to their default values and persists them to disk.
    /// </summary>
    /// <returns>A task representing the reset and save operation.</returns>
    public async Task ResetToDefaults()
    {
        await Update(new Setting()).ConfigureAwait(false);
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
}
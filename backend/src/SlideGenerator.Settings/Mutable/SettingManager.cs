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

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Settings.Mutable;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
///     Persists to <c>appsettings.json</c> under the <c>"Application"</c> section.
/// </summary>
/// <param name="logger">The logger instance.</param>
/// <param name="filePath">
///     Overrides the settings file path; used only by tests to avoid touching the real user settings
///     file. When <see langword="null" />, the real settings path is used.
/// </param>
internal sealed class SettingManager(
    ILogger<SettingManager>? logger = null,
    string? filePath = null)
    : ISettingManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    ///     Gets the full file path where settings are stored.
    /// </summary>
    private string FilePath => filePath ?? NameAndPaths.SettingsFile.GetFilePath(".json");

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
            var root = JsonSerializer.Deserialize<AppSettingsRoot>(source, JsonOptions);
            Current = root?.Application ?? new Setting();
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

            var content = JsonSerializer.Serialize(new AppSettingsRoot(Current), JsonOptions);
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

    /// <summary>The <c>appsettings.json</c> root shape: settings live under the <c>"Application"</c> key.</summary>
    private sealed record AppSettingsRoot(Setting Application);
}

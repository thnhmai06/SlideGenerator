/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
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
using SlideGenerator.Cryptography.Application.Abstractions;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Rules;

namespace SlideGenerator.Settings.Infrastructure.Services;

/// <summary>
///     Manages the loading, saving, and state of the application's <see cref="Setting" /> configuration.
/// </summary>
/// <param name="serializer">The serializer used to persist settings to disk.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class SettingManager(IEncrypter encrypter, ISerializer serializer, ISystemLogger logger)
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
            logger.Information("Setting file not found at {Path}. Using default settings.", FilePath);
            return false;
        }

        try
        {
            logger.Debug("Loading settings from {Path}", FilePath);
            var source = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            var loaded = serializer.Deserialize<Setting>(source);

            // Decrypt sensitive data
            if (!string.IsNullOrEmpty(loaded.Network.Proxy.Password))
            {
                loaded = loaded with
                {
                    Network = loaded.Network with
                    {
                        Proxy = loaded.Network.Proxy with
                        {
                            Password = encrypter.Decrypt(loaded.Network.Proxy.Password)
                        }
                    }
                };
            }

            Current = loaded;
            logger.Information("Successfully loaded settings from {Path}", FilePath);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error loading settings from {Path}. Resetting to defaults.", FilePath);
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
            logger.Debug("Saving settings to {Path}", FilePath);
            var folderName = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folderName)) Directory.CreateDirectory(folderName);

            // Encrypt sensitive data before serialization
            var toSave = Current;
            if (!string.IsNullOrEmpty(toSave.Network.Proxy.Password))
            {
                toSave = toSave with
                {
                    Network = toSave.Network with
                    {
                        Proxy = toSave.Network.Proxy with
                        {
                            Password = encrypter.Encrypt(toSave.Network.Proxy.Password)
                        }
                    }
                };
            }

            var content = serializer.Serialize(toSave);
            await File.WriteAllTextAsync(FilePath, content).ConfigureAwait(false);
            logger.Information("Successfully saved settings to {Path}", FilePath);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error saving settings to {Path}", FilePath);
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
}






/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: ISettingManager.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Domain.Entities;

namespace SlideGenerator.Settings.Application.Abstractions;

/// <summary>
///     Defines the contract for managing the application settings lifecycle:
///     load from disk, persist to disk, update in memory, and reset to defaults.
/// </summary>
public interface ISettingManager : ISettingProvider
{
    /// <summary>Loads settings from the persisted YAML file into memory.</summary>
    /// <returns><see langword="true" /> if the file existed and was loaded; <see langword="false" /> if defaults were used.</returns>
    Task<bool> Load();

    /// <summary>Persists the current in-memory settings to disk.</summary>
    Task Save();

    /// <summary>Resets all settings to factory defaults and persists them to disk.</summary>
    Task ResetToDefaults();

    /// <summary>
    ///     Replaces the current settings with <paramref name="newSetting" /> and persists them to disk.
    /// </summary>
    Task Update(Setting newSetting);
}
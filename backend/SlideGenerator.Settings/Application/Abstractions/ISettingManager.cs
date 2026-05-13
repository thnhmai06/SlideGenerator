/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: ISettingManager.cs
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







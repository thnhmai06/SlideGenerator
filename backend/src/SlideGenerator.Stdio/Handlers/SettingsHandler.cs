/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: SettingsHandler.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Stdio.Handlers.Models;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>settings.*</c> JSON-RPC methods: get, update, and resetToDefaults.
/// </summary>
public sealed class SettingsHandler(
    ISettingManager settingManager,
    ISettingProvider settingProvider)
{
    /// <summary>
    ///     Returns the current application settings wrapped in a <see cref="SettingsDto" />
    ///     that also carries the runtime-only <c>requiresCredentialReentry</c> flag.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="SettingsDto" /> envelope reflecting the current configuration.</returns>
    public Task<SettingsDto> GetAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new SettingsDto(settingProvider.Current, settingManager.RequiresCredentialReentry));
    }

    /// <summary>
    ///     Applies a settings update from the client and persists the new settings to disk.
    ///     The client must supply the complete settings object.
    /// </summary>
    /// <param name="setting">The full settings payload received from the client.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating whether the update succeeded.</returns>
    public async Task<bool> UpdateAsync(Setting setting, CancellationToken ct)
    {
        await settingManager.Update(setting).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Resets all settings to their factory defaults and persists the result to disk.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> ResetToDefaultsAsync(CancellationToken ct)
    {
        await settingManager.ResetToDefaults().ConfigureAwait(false);
        return true;
    }
}
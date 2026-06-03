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
using SlideGenerator.Settings.Domain.Models;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>settings.*</c> JSON-RPC methods.
///     Root methods operate on the full <see cref="Setting" /> object.
///     Section methods (<c>settings.performance.*</c>, <c>settings.network.*</c>) operate on individual sub-settings.
/// </summary>
public sealed class SettingsHandler(
    ISettingManager settingManager,
    ISettingProvider settingProvider,
    ISettingCalibrator settingCalibrator)
{
    #region Root

    /// <summary>Returns the current application settings.</summary>
    /// <param name="ct">A cancellation token.</param>
    public Task<Setting> GetAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(settingProvider.Current);
    }

    /// <summary>
    ///     Applies a settings update from the client and persists the new settings to disk.
    ///     The client must supply the complete settings object.
    /// </summary>
    /// <param name="setting">The full settings payload received from the client.</param>
    /// <returns>A <see cref="bool" /> indicating whether the update succeeded.</returns>
    public async Task<bool> UpdateAsync(Setting setting)
    {
        await settingManager.Update(setting).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Resets all settings to their factory defaults and persists the result to disk.
    /// </summary>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> ResetAsync()
    {
        await settingManager.ResetToDefaults().ConfigureAwait(false);
        return true;
    }

    #endregion

    #region Performance

    /// <summary>Returns the current <see cref="Setting.PerformanceSetting" />.</summary>
    /// <param name="ct">A cancellation token.</param>
    public Task<Setting.PerformanceSetting> GetPerformanceAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(settingProvider.Current.Performance);
    }

    /// <summary>
    ///     Replaces the performance section and persists to disk.
    ///     All other sections remain unchanged.
    /// </summary>
    /// <param name="performance">The new performance configuration.</param>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> UpdatePerformanceAsync(Setting.PerformanceSetting performance)
    {
        await settingManager.Update(settingProvider.Current with { Performance = performance }).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Resets only the performance section to factory defaults and persists to disk.
    ///     All other sections remain unchanged.
    /// </summary>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> ResetPerformanceAsync()
    {
        await settingManager.Update(settingProvider.Current with { Performance = new Setting.PerformanceSetting() }).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Probes hardware and network conditions to compute recommended performance settings.
    ///     Does not persist the result — the client applies it via <c>settings.performance.update</c>.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="PerformanceCalibration" /> containing raw probe data and the recommended configuration.</returns>
    public Task<PerformanceCalibration> CalibratePerformanceAsync(CancellationToken ct)
    {
        return settingCalibrator.CalibratePerformanceAsync(ct);
    }

    #endregion

    #region Network

    /// <summary>Returns the current <see cref="Setting.NetworkSetting" />.</summary>
    /// <param name="ct">A cancellation token.</param>
    public Task<Setting.NetworkSetting> GetNetworkAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(settingProvider.Current.Network);
    }

    /// <summary>
    ///     Replaces the network section and persists to disk.
    ///     All other sections remain unchanged.
    /// </summary>
    /// <param name="network">The new network configuration.</param>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> UpdateNetworkAsync(Setting.NetworkSetting network)
    {
        await settingManager.Update(settingProvider.Current with { Network = network }).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Resets only the network section to factory defaults and persists to disk.
    ///     All other sections remain unchanged.
    /// </summary>
    /// <returns>A <see cref="bool" /> indicating success.</returns>
    public async Task<bool> ResetNetworkAsync()
    {
        await settingManager.Update(settingProvider.Current with { Network = new Setting.NetworkSetting() }).ConfigureAwait(false);
        return true;
    }

    #endregion
}

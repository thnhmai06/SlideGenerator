/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ThemeService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Desktop.Services.Theme;

/// <summary>
///     Applies <see cref="Setting.AppearanceSetting.Theme" /> to the running application, and persists changes
///     made at runtime (e.g. from the Settings page) back through <see cref="ISettingManager" />.
/// </summary>
public interface IThemeService
{
    /// <summary>
    ///     Applies <see cref="ISettingProvider.Current" />'s <see cref="Setting.AppearanceSetting.Theme" /> to
    ///     <see cref="Application.RequestedThemeVariant" /> immediately. Call once after settings are loaded
    ///     at startup, and again any time the setting changes outside <see cref="SetThemeAsync" />.
    /// </summary>
    void ApplyFromSettings();

    /// <summary>
    ///     Persists <paramref name="mode" /> to settings and applies it to the running application immediately
    ///     — no restart needed.
    /// </summary>
    Task SetThemeAsync(ThemeMode mode);
}

/// <inheritdoc cref="IThemeService" />
public sealed class ThemeService(ISettingManager settingManager) : IThemeService
{
    /// <inheritdoc />
    public void ApplyFromSettings()
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = ToThemeVariant(settingManager.Current.Appearance.Theme);
    }

    /// <inheritdoc />
    public async Task SetThemeAsync(ThemeMode mode)
    {
        var current = settingManager.Current;
        await settingManager.Update(current with { Appearance = current.Appearance with { Theme = mode } })
            .ConfigureAwait(false);
        ApplyFromSettings();
    }

    private static ThemeVariant ToThemeVariant(ThemeMode mode)
    {
        return mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}

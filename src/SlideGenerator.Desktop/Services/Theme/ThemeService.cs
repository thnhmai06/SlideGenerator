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
    ///     <see cref="Application.RequestedThemeVariant" />, and its
    ///     <see cref="Setting.AppearanceSetting.ReducedMotion" /> to the app's <c>MotionUi</c>/<c>MotionBrand</c>
    ///     duration resources (see <see cref="ApplyReducedMotion" />), immediately. Call once after settings are
    ///     loaded at startup, and again any time either setting changes outside <see cref="SetThemeAsync" />.
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
    private static TimeSpan? _originalMotionUi;
    private static TimeSpan? _originalMotionBrand;

    /// <inheritdoc />
    public void ApplyFromSettings()
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = ToThemeVariant(settingManager.Current.Appearance.Theme);
        ApplyReducedMotion(settingManager.Current.Appearance.ReducedMotion);
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

    /// <summary>
    ///     Zeroes (or restores) the <c>MotionUi</c>/<c>MotionBrand</c> <see cref="TimeSpan" /> resources
    ///     declared in <c>Tokens.axaml</c> — every <c>DynamicResource</c> binding to either (button press
    ///     feedback, the splash lockup animation, etc.) picks up the change immediately since Avalonia's
    ///     resource lookup checks the top-level <see cref="Application.Resources" /> dictionary before any
    ///     merged one. The original values are captured once, from whatever <c>Tokens.axaml</c> declares, so
    ///     toggling reduced motion back off restores the real design-token durations rather than a hardcoded
    ///     duplicate. Known gap: <c>ShellView</c>'s page <c>CrossFade</c> transition sets its <c>Duration</c> as
    ///     a literal (not bindable — a plain CLR property, see its own comment) so it keeps animating regardless
    ///     of this setting.
    /// </summary>
    private static void ApplyReducedMotion(bool reduced)
    {
        var resources = Application.Current!.Resources;
        _originalMotionUi ??= (TimeSpan)resources["MotionUi"]!;
        _originalMotionBrand ??= (TimeSpan)resources["MotionBrand"]!;
        resources["MotionUi"] = reduced ? TimeSpan.Zero : _originalMotionUi.Value;
        resources["MotionBrand"] = reduced ? TimeSpan.Zero : _originalMotionBrand.Value;
    }
}

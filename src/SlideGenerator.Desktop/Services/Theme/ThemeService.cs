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
    private static TimeSpan? _originalMotionMicro;
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
        // ConfigureAwait(true) deliberately, unlike SlideGenerator.Settings' own module convention — this
        // method is called fire-and-forget from a UI event handler (SettingsViewModel.OnThemeChanged) with no
        // continuation to observe a fault, and ApplyFromSettings() below sets Application.RequestedThemeVariant,
        // an Avalonia styled property that must be touched from the UI thread. Without this, execution resumes
        // on a thread-pool thread after the await (since settingManager.Update's own internals use
        // ConfigureAwait(false)), the property set silently no-ops off-thread, and theme switching does
        // nothing — confirmed via Avalonia DevTools (Application.RequestedThemeVariant stayed "Default" after
        // selecting Dark in Settings).
        var current = settingManager.Current;
        await settingManager.Update(current with { Appearance = current.Appearance with { Theme = mode } })
            .ConfigureAwait(true);
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
    ///     Zeroes (or restores) the <c>MotionMicro</c>/<c>MotionUi</c>/<c>MotionBrand</c> <see cref="TimeSpan" />
    ///     resources declared in <c>Tokens.axaml</c> — every <c>DynamicResource</c> binding to any of them
    ///     (hover/press feedback, page transitions, the splash lockup animation, etc.) picks up the change
    ///     immediately, since a direct-write on the top-level <see cref="Application.Resources" /> dictionary
    ///     shadows the merged <c>Tokens.axaml</c> entry for <c>DynamicResource</c> lookups. The original values
    ///     are read once via <see cref="Application.TryGetResource(object,ThemeVariant?,out object?)" /> — which,
    ///     unlike the dictionary indexer, actually walks <see cref="Application.Resources" />'s merged
    ///     dictionaries — so toggling reduced motion back off restores the real design-token durations rather
    ///     than a hardcoded duplicate. <c>MainWindow</c>/<c>ShellView</c> read <see cref="GetMotionResource" />
    ///     once at construction to build their page <c>CrossFade</c> transitions in code (a plain CLR property,
    ///     not bindable — see their own comments), so those also start in the right state; toggling the setting
    ///     later re-applies to already-running transitions via the same shadowed resource, EXCEPT the page
    ///     transition's own <c>Duration</c>, which was already captured by value at construction and does not
    ///     re-read after that (live re-assignment on toggle is out of scope here).
    /// </summary>
    private static void ApplyReducedMotion(bool reduced)
    {
        var app = Application.Current!;
        _originalMotionMicro ??= GetMotionResource(app, "MotionMicro");
        _originalMotionUi ??= GetMotionResource(app, "MotionUi");
        _originalMotionBrand ??= GetMotionResource(app, "MotionBrand");

        app.Resources["MotionMicro"] = reduced ? TimeSpan.Zero : _originalMotionMicro.Value;
        app.Resources["MotionUi"] = reduced ? TimeSpan.Zero : _originalMotionUi.Value;
        app.Resources["MotionBrand"] = reduced ? TimeSpan.Zero : _originalMotionBrand.Value;
    }

    /// <summary>
    ///     Reads a <c>Tokens.axaml</c> duration resource via <see cref="Application.TryGetResource" /> — unlike
    ///     the dictionary indexer, this walks <see cref="Application.Resources" />'s merged dictionaries, which
    ///     is where <c>Tokens.axaml</c> actually lives (see <see cref="ApplyReducedMotion" />'s remarks). Shared
    ///     with <see cref="Shell.MainWindow" />/<see cref="Shell.ShellView" /> so their code-built page
    ///     transitions start with the correct (possibly already-reduced) duration instead of a hardcoded one.
    /// </summary>
    internal static TimeSpan GetMotionResource(Application app, string key)
    {
        return app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is TimeSpan duration
            ? duration
            : TimeSpan.Zero;
    }
}

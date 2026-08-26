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

using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
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

    /// <summary>
    ///     Same as <see cref="SetThemeAsync" />, but the switch is revealed as a circle expanding outward from
    ///     <paramref name="origin" /> (window-client coordinates) instead of an instant repaint — the toolbar's
    ///     theme button uses this so the switch reads as "spreading from the button you clicked", matching the
    ///     product brief's Unsloth-inspired reveal. Falls back to the instant <see cref="SetThemeAsync" /> when
    ///     <c>ReducedMotion</c> is on, there is no main window, or the window has zero client area.
    /// </summary>
    Task SetThemeAnimatedAsync(ThemeMode mode, Point origin);
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

    /// <inheritdoc />
    public async Task SetThemeAnimatedAsync(ThemeMode mode, Point origin)
    {
        var app = Application.Current;
        var window = (app?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var overlayLayer = window is null ? null : OverlayLayer.GetOverlayLayer(window);
        var clientSize = window?.ClientSize ?? default;

        // Fall back to the instant switch whenever the reveal can't run for real, rather than skip the theme
        // change itself: no window/overlay layer to paint into, a not-yet-laid-out (zero-size) window, or
        // ReducedMotion (checked last, via the same MotionBrand=0 signal ApplyReducedMotion sets — no separate
        // "is reduced motion on" query needed since that's exactly what zeroing the resource means).
        if (app is null || window is null || overlayLayer is null || clientSize.Width <= 0 || clientSize.Height <= 0
            || GetMotionResource(app, "MotionBrand") is var duration && duration == TimeSpan.Zero)
        {
            await SetThemeAsync(mode).ConfigureAwait(true);
            return;
        }

        // 1. Snapshot the OLD theme's pixels (must happen before the swap below — there is no way to render
        //    the NEW theme before it's actually applied) and paint them back over the live window.
        var scaling = window.RenderScaling;
        var pixelSize = new PixelSize(
            (int)Math.Ceiling(clientSize.Width * scaling), (int)Math.Ceiling(clientSize.Height * scaling));
        using var snapshot = new RenderTargetBitmap(pixelSize, new Vector(96 * scaling, 96 * scaling));
        snapshot.Render(window);

        var overlay = new Avalonia.Controls.Image
        {
            Source = snapshot,
            Stretch = Stretch.Fill,
            IsHitTestVisible = false,
            Width = clientSize.Width,
            Height = clientSize.Height
        };
        overlayLayer.Children.Add(overlay);

        try
        {
            // 2. Yield exactly one composited frame so the overlay is actually on screen BEFORE the restyle
            //    below runs underneath it — the entire effect depends on this ordering. Doing the swap first
            //    (or on the same tick) lets the new theme's colors flash through for one frame before the
            //    overlay catches up, defeating the reveal. DispatcherPriority.Render is deliberate: Background
            //    or lower can still land before the compositor's next paint.
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            // 3. Apply the real theme change now, hidden behind the opaque overlay.
            var current = settingManager.Current;
            await settingManager.Update(current with { Appearance = current.Appearance with { Theme = mode } })
                .ConfigureAwait(true);
            ApplyFromSettings();

            // 4. Animate a growing hole in the overlay from `origin` out to the farthest corner, revealing the
            //    new theme underneath as the hole grows — RequestAnimationFrame, not a DispatcherTimer, so the
            //    step is synced to the actual render tick (see TopLevel.RequestAnimationFrame's own docs).
            var maxRadius = DistanceToFarthestCorner(origin, clientSize);
            var stopwatch = Stopwatch.StartNew();
            var completed = new TaskCompletionSource();

            void Tick(TimeSpan _)
            {
                var t = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                var eased = 1 - Math.Pow(1 - t, 3); // cubic ease-out, matching MotionBrand's easing elsewhere
                var radius = eased * maxRadius;
                overlay.Clip = new CombinedGeometry(GeometryCombineMode.Exclude,
                    new RectangleGeometry(new Rect(overlay.Bounds.Size)),
                    new EllipseGeometry(new Rect(origin.X - radius, origin.Y - radius, radius * 2, radius * 2)));

                if (t < 1.0) window.RequestAnimationFrame(Tick);
                else completed.TrySetResult();
            }

            window.RequestAnimationFrame(Tick);
            await completed.Task.ConfigureAwait(true);
        }
        finally
        {
            overlayLayer.Children.Remove(overlay);
        }
    }

    private static double DistanceToFarthestCorner(Point origin, Size size)
    {
        ReadOnlySpan<Point> corners =
        [
            new Point(0, 0), new Point(size.Width, 0), new Point(0, size.Height), new Point(size.Width, size.Height)
        ];
        var max = 0.0;
        foreach (var corner in corners)
        {
            var distance = Math.Sqrt(Math.Pow(corner.X - origin.X, 2) + Math.Pow(corner.Y - origin.Y, 2));
            if (distance > max) max = distance;
        }

        return max;
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
    ///     than a hardcoded duplicate. <c>MainWindow</c>/<c>ShellView</c> call <see cref="BuildPageTransition" />
    ///     once at construction to build their page transition in code (a plain CLR property, not bindable —
    ///     see their own comments), so those also start in the right state; toggling the setting
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

    /// <summary>
    ///     Builds the page transition <c>MainWindow</c>/<c>ShellView</c> assign to their content host in code
    ///     (see <see cref="GetMotionResource" />'s remarks for why it can't be plain XAML) — a fade plus a
    ///     subtle upward slide, composed via <see cref="CompositePageTransition" /> rather than a hand-rolled
    ///     <see cref="IPageTransition" />, since Avalonia already ships exactly this composition primitive.
    /// </summary>
    internal static IPageTransition BuildPageTransition(Application app)
    {
        var duration = GetMotionResource(app, "MotionUi");
        var composite = new CompositePageTransition();
        composite.PageTransitions.Add(new CrossFade(duration));
        composite.PageTransitions.Add(new PageSlide(duration, PageSlide.SlideAxis.Vertical));
        return composite;
    }
}

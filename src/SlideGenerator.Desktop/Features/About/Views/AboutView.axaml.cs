/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: AboutView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SlideGenerator.Desktop.Features.About.Models;
using SlideGenerator.Desktop.Features.About.ViewModels;

namespace SlideGenerator.Desktop.Features.About.Views;

/// <summary>View for <see cref="AboutViewModel" />. Replays <see cref="Components.BrandLockup" /> and
///     (re)triggers the data load every time this page becomes visible (plan §5.7: "Play() mỗi lần điều hướng
///     tới") — unlike <c>SplashView</c>, <c>Loaded</c> stays subscribed rather than firing once, since
///     <c>ShellViewModel</c> caches and reattaches this same instance on every visit rather than recreating
///     it. <see cref="AboutViewModel.LoadAsync" /> itself is idempotent, so re-triggering it here is harmless.</summary>
public sealed partial class AboutView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public AboutView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _ = Lockup.PlayAsync();
        if (DataContext is AboutViewModel vm) _ = vm.LoadAsync();
    }

    private void OnDeveloperPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is StyledElement { DataContext: Contributor c })
            Process.Start(new ProcessStartInfo(c.ProfileUrl) { UseShellExecute = true });
    }

    private void OnSupporterPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is StyledElement { DataContext: Supporter s })
            Process.Start(new ProcessStartInfo(s.ProfileUrl) { UseShellExecute = true });
    }
}

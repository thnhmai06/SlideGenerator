/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TitleToolbar.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SlideGenerator.Desktop.Shell;

/// <summary>
///     Code-behind for the caption buttons (<c>WindowDecorationProperties.ElementRole</c> alone does not
///     perform the minimize/maximize/close action — confirmed via the P0 spike: it only affects non-client
///     hit-testing, cursor, Snap Layouts eligibility, not the click itself — so each button still needs an
///     explicit handler that calls the platform API) and the theme-toggle button (needs the button's own
///     on-screen position as the reveal's origin — a view-layer concept, not something
///     <see cref="ShellViewModel" /> should hold as state — so this calls
///     <see cref="ShellViewModel.ToggleThemeAsync" /> directly instead of going through a bound Command).
///     Everything else in <see cref="TitleToolbar" /> is a plain data-bound command.
/// </summary>
public sealed partial class TitleToolbar : UserControl
{
    /// <summary>Constructs the toolbar and loads its XAML.</summary>
    public TitleToolbar()
    {
        InitializeComponent();
    }

    private Window? OwnerWindow => TopLevel.GetTopLevel(this) as Window;

    private void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control button || DataContext is not ShellViewModel viewModel) return;
        var window = OwnerWindow;
        var center = new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        var origin = window is not null ? button.TranslatePoint(center, window) : null;
        _ = viewModel.ToggleThemeAsync(origin);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        if (OwnerWindow is { } window) window.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        if (OwnerWindow is not { } window) return;
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        OwnerWindow?.Close();
    }
}

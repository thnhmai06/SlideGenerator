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

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SlideGenerator.Desktop.Shell;

/// <summary>
///     Code-behind for the caption buttons only — everything else in <see cref="TitleToolbar" /> is a plain
///     data-bound command. <c>WindowDecorationProperties.ElementRole</c> alone does not perform the
///     minimize/maximize/close action (confirmed via the P0 spike: it only affects non-client hit-testing —
///     cursor, Snap Layouts eligibility — not the click itself), so each button still needs an explicit
///     handler that calls the platform API.
/// </summary>
public sealed partial class TitleToolbar : UserControl
{
    /// <summary>Constructs the toolbar and loads its XAML.</summary>
    public TitleToolbar()
    {
        InitializeComponent();
    }

    private Window? OwnerWindow => TopLevel.GetTopLevel(this) as Window;

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

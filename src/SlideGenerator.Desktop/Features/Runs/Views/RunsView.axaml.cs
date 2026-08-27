/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RunsView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using SlideGenerator.Desktop.Features.Runs.ViewModels;

namespace SlideGenerator.Desktop.Features.Runs.Views;

/// <summary>View for <see cref="ViewModels.RunsViewModel" />.</summary>
public sealed partial class RunsView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public RunsView()
    {
        InitializeComponent();
    }

    // ponytail: scrolls to the bottom on every layout pass rather than tracking whether the user had
    // already scrolled away from the bottom — a log pane the user is actively scrolling back through mid-run
    // would get yanked down again on the next line. Upgrade to at-bottom tracking if that's ever reported.
    private void OnLogItemsLayoutUpdated(object? sender, EventArgs e)
    {
        LogScroll.ScrollToEnd();
    }

    private async void OnCopyLogsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RunsViewModel { SelectedRequest: { } request }) return;
        var text = new StringBuilder();
        foreach (var log in request.Logs) text.AppendLine($"{log.Timestamp} {log.Level} {log.Info}");

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(text.ToString());
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: MainWindow.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using SlideGenerator.Desktop.Services.Theme;

namespace SlideGenerator.Desktop.Shell;

/// <summary>Hosts the splash screen, then the shell, behind a single cross-fade transition.</summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Initializes a new instance of the <see cref="MainWindow" /> class.</summary>
    public MainWindow()
    {
        InitializeComponent();

        // Built in code, not XAML: CrossFade.Duration is a plain CLR property (not a StyledProperty), so it
        // cannot bind to {DynamicResource MotionUi} — reading the resource once here at least respects
        // Appearance.ReducedMotion as it stood at startup (see ThemeService.GetMotionResource's remarks).
        ContentHost.PageTransition = new CrossFade(ThemeService.GetMotionResource(Application.Current!, "MotionUi"));
    }
}
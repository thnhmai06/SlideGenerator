/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ShellView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls;
using SlideGenerator.Desktop.Services.Theme;

namespace SlideGenerator.Desktop.Shell;

/// <summary>Toolbar + page host for <see cref="ShellViewModel" />.</summary>
public sealed partial class ShellView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public ShellView()
    {
        InitializeComponent();

        // Built in code, not XAML — see MainWindow's constructor comment for why.
        PageHost.PageTransition = ThemeService.BuildPageTransition(Application.Current!);
    }
}

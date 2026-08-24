/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: SplashView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia.Controls;
using Avalonia.Threading;

namespace SlideGenerator.Desktop.Shell;

/// <summary>The startup lockup animation — see <see cref="SplashViewModel" /> for why it has no state.</summary>
public sealed partial class SplashView : UserControl
{
    /// <summary>Constructs the view, loads its XAML, and arms the entrance transition.</summary>
    public SplashView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        // Toggling the class in the same tick as Loaded can land on the same layout pass as the initial
        // (pre-transition) values, so the Transitions declared in SplashView.axaml would have nothing to
        // animate from. Posting at Loaded priority defers one pass, guaranteeing the "before" state has
        // actually rendered once before the "after" state is applied.
        Dispatcher.UIThread.Post(() =>
        {
            MarkImage.Classes.Add("revealed");
            WordImage.Classes.Add("revealed");
        }, DispatcherPriority.Loaded);
    }
}

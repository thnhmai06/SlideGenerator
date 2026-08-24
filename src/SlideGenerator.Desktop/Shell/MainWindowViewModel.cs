/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: MainWindowViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;

namespace SlideGenerator.Desktop.Shell;

/// <summary>
///     Owns the single top-level content swap for the app's one window: <see cref="SplashViewModel" /> while
///     startup init is still running, then <see cref="ShellViewModel" /> once it completes (see
///     <c>App.axaml.cs</c>'s startup sequencing). A separate, tiny ViewModel rather than folding this into
///     <see cref="ShellViewModel" /> — splash lifecycle and page navigation are different concerns, and
///     <see cref="ShellViewModel" /> should not need to know it was ever preceded by a splash screen.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private ObservableObject? _currentContent;
}

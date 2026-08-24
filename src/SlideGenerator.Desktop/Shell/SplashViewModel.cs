/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: SplashViewModel.cs
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
///     Marker ViewModel for <see cref="SplashView" /> — the lockup animation is purely declarative
///     (<c>Style.Animations</c> that auto-play once the view loads), so there is no state to hold here. Exists
///     only so <see cref="ViewLocator" /> and <see cref="MainWindowViewModel.CurrentContent" /> have a
///     ViewModel-shaped value to bind, consistent with every other page.
/// </summary>
public sealed class SplashViewModel : ObservableObject;

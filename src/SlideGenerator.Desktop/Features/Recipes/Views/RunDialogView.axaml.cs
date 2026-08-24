/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RunDialogView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia.Controls;
using SlideGenerator.Desktop.Features.Recipes.ViewModels;

namespace SlideGenerator.Desktop.Features.Recipes.Views;

/// <summary>View for <see cref="RunDialogViewModel" />. Closes itself when the ViewModel requests it.</summary>
public sealed partial class RunDialogView : Window
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public RunDialogView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is RunDialogViewModel vm) vm.RequestClose += started => Close(started);
        };
    }
}

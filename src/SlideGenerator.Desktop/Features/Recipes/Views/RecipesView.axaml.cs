/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipesView.axaml.cs
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

/// <summary>View for <see cref="ViewModels.RecipesViewModel" />.</summary>
public sealed partial class RecipesView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public RecipesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RecipesViewModel vm) vm.FocusSearchRequested += () => SearchBox.Focus();
    }
}

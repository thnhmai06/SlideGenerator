/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TextBindingsView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia.Controls;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Views;

/// <summary>View for <see cref="TextBindingsViewModel" />.</summary>
public sealed partial class TextBindingsView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public TextBindingsView()
    {
        InitializeComponent();
    }

    private void OnColumnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: string column, DataContext: TextBindingRowViewModel row } &&
            DataContext is TextBindingsViewModel vm)
            vm.SetColumn(row, column);
    }
}

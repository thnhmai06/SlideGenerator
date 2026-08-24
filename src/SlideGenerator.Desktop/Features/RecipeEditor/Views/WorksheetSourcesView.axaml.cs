/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: WorksheetSourcesView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Views;

/// <summary>View for <see cref="WorksheetSourcesViewModel" />.</summary>
public sealed partial class WorksheetSourcesView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public WorksheetSourcesView()
    {
        InitializeComponent();
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: WorksheetSourceRowViewModel row } && DataContext is WorksheetSourcesViewModel vm)
            vm.RemoveCommand.Execute(row);
    }
}

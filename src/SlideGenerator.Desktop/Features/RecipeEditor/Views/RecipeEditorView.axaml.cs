/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipeEditorView.axaml.cs
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
using SlideGenerator.Image.Cropping;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Views;

/// <summary>View for <see cref="RecipeEditorViewModel" /> — assembles the mapping navigator, canvas, and
///     text/source panels built in P4.1-P4.3 into one page.</summary>
public sealed partial class RecipeEditorView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public RecipeEditorView()
    {
        InitializeComponent();
    }

    private void OnNavigatorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is not MappingEditSession session) return;
        if (DataContext is not RecipeEditorViewModel vm) return;

        vm.SelectSessionCommand.Execute(session);
    }

    private void OnMoveUpClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: MappingEditSession session } && DataContext is RecipeEditorViewModel vm)
            vm.MoveMappingUpCommand.Execute(session);
    }

    private void OnMoveDownClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: MappingEditSession session } && DataContext is RecipeEditorViewModel vm)
            vm.MoveMappingDownCommand.Execute(session);
    }

    private void OnRemoveMappingClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: MappingEditSession session } && DataContext is RecipeEditorViewModel vm)
            vm.RemoveMappingCommand.Execute(session);
    }

    private void OnRoiMoveUpClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: RoiOption option } && InspectorPanel.DataContext is ShapeOverlayViewModel overlay &&
            DataContext is RecipeEditorViewModel vm)
        {
            overlay.MoveRoiUpCommand.Execute(option);
            vm.NotifyEdited();
        }
    }

    private void OnRoiMoveDownClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: RoiOption option } && InspectorPanel.DataContext is ShapeOverlayViewModel overlay &&
            DataContext is RecipeEditorViewModel vm)
        {
            overlay.MoveRoiDownCommand.Execute(option);
            vm.NotifyEdited();
        }
    }

    private void OnRoiRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: RoiOption option } && InspectorPanel.DataContext is ShapeOverlayViewModel overlay &&
            DataContext is RecipeEditorViewModel vm)
        {
            overlay.RemoveRoiCommand.Execute(option);
            vm.NotifyEdited();
        }
    }

    private void OnPickFallbackImageClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RecipeEditorViewModel vm) return;
        if (InspectorPanel.DataContext is not ShapeOverlayViewModel overlay) return;

        vm.PickFallbackImageCommand.Execute(overlay);
    }
}

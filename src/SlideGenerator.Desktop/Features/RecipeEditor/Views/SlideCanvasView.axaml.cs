/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: SlideCanvasView.axaml.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Views;

/// <summary>View for <see cref="SlideCanvasViewModel" />. Pushes the root panel's actual rendered size into
///     the ViewModel on every layout change, since Avalonia has no bindable "my own size" source — that size
///     drives <see cref="SlideCanvasViewModel.CanvasSize" /> for the overlay scaling math.</summary>
public sealed partial class SlideCanvasView : UserControl
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public SlideCanvasView()
    {
        InitializeComponent();
        RootPanel.PropertyChanged += OnRootPanelPropertyChanged;
    }

    private void OnRootPanelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty && DataContext is SlideCanvasViewModel vm)
            vm.CanvasSize = RootPanel.Bounds.Size;
    }

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is StyledElement { DataContext: ShapeOverlayViewModel overlay } && DataContext is SlideCanvasViewModel vm)
            vm.SelectCommand.Execute(overlay);
    }
}

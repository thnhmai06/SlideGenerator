/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TemplatePickerView.axaml.cs
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
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Views;

/// <summary>View for <see cref="TemplatePickerViewModel" />. Closes itself when the ViewModel requests it.</summary>
public sealed partial class TemplatePickerView : Window
{
    /// <summary>Constructs the view and loads its XAML.</summary>
    public TemplatePickerView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is TemplatePickerViewModel vm) vm.RequestClose += picked => Close(picked);
        };
    }

    private void OnSlidePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is StyledElement { DataContext: TemplateSlideRow row } && DataContext is TemplatePickerViewModel vm)
            vm.SelectedSlide = row;
    }
}

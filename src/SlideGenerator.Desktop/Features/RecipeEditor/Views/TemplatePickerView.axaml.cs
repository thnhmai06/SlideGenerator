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

using Avalonia.Controls;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Views;

/// <summary>View for <see cref="TemplatePickerViewModel" />. Closes itself when the ViewModel requests it.
///     Slide selection itself is a plain <c>ListBox.SelectedItem</c> binding (P4c) — no code-behind needed.</summary>
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
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ColumnCheckboxViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>One worksheet column's checkbox in a <see cref="WorksheetSourceRowViewModel" />'s <c>UsedColumns</c> list.</summary>
public sealed partial class ColumnCheckboxViewModel(string name, bool isChecked) : ObservableObject
{
    /// <summary>Gets the column header name.</summary>
    public string Name { get; } = name;

    [ObservableProperty] private bool _isChecked = isChecked;
}

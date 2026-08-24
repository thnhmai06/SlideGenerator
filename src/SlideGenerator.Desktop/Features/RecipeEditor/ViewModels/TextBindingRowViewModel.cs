/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TextBindingRowViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     One placeholder row in the "CHỮ" list (plan §5.2) — a <c>{{Placeholder}}</c> tag and its
///     <see cref="Binding" /> state. <see cref="DropdownOptions" /> narrows to <see cref="BindingDisplay.Candidates" />
///     when <see cref="BindingDisplayState.NeedsSelection" /> (plan: "dropdown đã lọc sẵn các ứng viên"),
///     otherwise offers every column so the user can override any auto-bound suggestion.
/// </summary>
public sealed partial class TextBindingRowViewModel(string placeholder, BindingDisplay binding, IReadOnlyList<string> allColumns) : ObservableObject
{
    /// <summary>Gets the placeholder tag name (without the surrounding <c>{{ }}</c>).</summary>
    public string Placeholder { get; } = placeholder;

    [ObservableProperty] private BindingDisplay _binding = binding;

    /// <summary>Gets every column visible to this mapping's worksheet sources, for the dropdown's full list.</summary>
    public IReadOnlyList<string> AllColumns { get; } = allColumns;

    /// <summary>Gets the columns the dropdown should actually offer right now.</summary>
    public IReadOnlyList<string> DropdownOptions =>
        Binding.State == BindingDisplayState.NeedsSelection ? Binding.Candidates : AllColumns;

    partial void OnBindingChanged(BindingDisplay value)
    {
        OnPropertyChanged(nameof(DropdownOptions));
    }
}

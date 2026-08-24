/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TextBindingsViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Owns one mapping's placeholder-to-column bindings (plan §5.2 "CHỮ" list) and the count summary shown in
///     Guided step ④ and the Advanced warning strip. Each placeholder gets exactly one column here — the
///     multi-column fallback chain <c>TextInstruction.Columns</c> actually supports is an inspector-only
///     "advanced" feature (plan: "chuỗi fallback là advanced, mở trong inspector"), out of scope for this list.
/// </summary>
public sealed partial class TextBindingsViewModel : ObservableObject
{
    private readonly HashSet<string> _touched = [];

    /// <summary>Gets the current placeholder rows.</summary>
    public ObservableCollection<TextBindingRowViewModel> Rows { get; } = [];

    /// <summary>Gets the count of each <see cref="BindingDisplayState" /> across <see cref="Rows" />.</summary>
    public (int Assigned, int Suggested, int NeedsSelection, int Unassigned) Summary =>
        BindingDisplayResolver.Summarize(Rows.Select(r => r.Binding).ToList());

    /// <summary>Loads one row per placeholder, deriving its <see cref="BindingDisplay" /> from any existing
    ///     <see cref="TextInstruction" /> and, failing that, an auto-bind suggestion against <paramref name="allColumns" />.</summary>
    public void Load(IReadOnlyList<string> placeholders, IReadOnlyList<TextInstruction> textInstructions, IReadOnlyList<string> allColumns)
    {
        _touched.Clear();
        Rows.Clear();
        foreach (var placeholder in placeholders)
        {
            var instruction = textInstructions.FirstOrDefault(i => i.Placeholders.Contains(placeholder));
            var existingColumn = instruction?.Columns.FirstOrDefault()?.ColumnName;
            var binding = BindingDisplayResolver.Resolve(placeholder, existingColumn, allColumns, touched: false);
            Rows.Add(new TextBindingRowViewModel(placeholder, binding, allColumns));
        }

        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>Sets (or overrides) a row's column, marking it touched so a Suggested state never reappears for it.</summary>
    public void SetColumn(TextBindingRowViewModel row, string column)
    {
        _touched.Add(row.Placeholder);
        row.Binding = new BindingDisplay(row.Placeholder, BindingDisplayState.Assigned, column, []);
        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>Projects every row with a column into the flat <see cref="TextInstruction" /> list a <c>Mapping</c> needs.</summary>
    public IReadOnlyList<TextInstruction> ToTextInstructions()
    {
        return Rows
            .Where(r => r.Binding.Column is not null)
            .Select(r => new TextInstruction(new HashSet<string> { r.Placeholder }, [new ColumnIdentifier(r.Binding.Column!)]))
            .ToList();
    }
}

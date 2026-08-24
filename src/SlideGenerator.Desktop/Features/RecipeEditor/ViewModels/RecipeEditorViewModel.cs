/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecipeEditorViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using RecipeModel = SlideGenerator.Recipe.Models.Recipe;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     Skeleton for the Recipe editor (P4.1) — owns the recipe under edit, the mapping currently shown in the
///     canvas/inspector, and dirty tracking. No view yet: <see cref="Models.BindingMatcher" /> and the canvas
///     built in P4.2+ consume this state; this phase only establishes the shape so later phases don't need to
///     restructure it.
/// </summary>
public sealed partial class RecipeEditorViewModel : ObservableObject
{
    [ObservableProperty] private RecipeModel _recipe = new([]);
    [ObservableProperty] private SlideGenerator.Recipe.Models.Mapping? _selectedMapping;
    [ObservableProperty] private bool _isDirty;

    // Transient DI + Initialize(...), matching RunDialogViewModel — the recipe under edit is a runtime value,
    // not a DI dependency, and this mirrors how the Run dialog already handles the same shape of problem.
    /// <summary>Loads the given recipe into the editor and clears the dirty flag.</summary>
    public void Initialize(RecipeModel recipe)
    {
        Recipe = recipe;
        SelectedMapping = recipe.Mappings.FirstOrDefault();
        IsDirty = false;
    }

    partial void OnRecipeChanged(RecipeModel value)
    {
        IsDirty = true;
    }
}

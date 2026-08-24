/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: MappingEditSession.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;

/// <summary>
///     One <see cref="Mapping" /> under edit, plus the "touched" placeholder/shape names that must survive
///     switching to another mapping and back (see <see cref="RecipeEditorViewModel" />) — a stable object
///     identity for the mapping navigator to select, unlike <see cref="Mapping" /> itself (a record, so its
///     value — and therefore its identity as a dictionary key — changes on every edit).
/// </summary>
public sealed partial class MappingEditSession(Mapping mapping) : ObservableObject
{
    [ObservableProperty] private Mapping _mapping = mapping;

    /// <summary>Gets the placeholder names the user has explicitly confirmed/changed a column for.</summary>
    public HashSet<string> TouchedPlaceholders { get; } = [];

    /// <summary>Gets the image-shape names the user has explicitly confirmed/changed a column for.</summary>
    public HashSet<string> TouchedShapes { get; } = [];

    /// <summary>Gets a short label for the mapping navigator — the template slide's index, since a
    ///     <see cref="Mapping" /> has no name of its own.</summary>
    public string Label => $"Slide {Mapping.Template.Slide.SlideIndex}";
}

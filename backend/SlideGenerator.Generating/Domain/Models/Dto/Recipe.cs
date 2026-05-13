/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: Recipe.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;

namespace SlideGenerator.Generating.Domain.Models.Dto;

/// <summary>
///     Represents a single mapping node that links multiple source sheets to a target slide template.
///     Contains specific instructions for text and image replacements for this mapping.
/// </summary>
/// <param name="Sheets">The set of source Excel worksheets.</param>
/// <param name="Slide">The target PowerPoint slide template.</param>
/// <param name="TextInstructions">The list of rules for mapping Excel columns to text placeholders.</param>
/// <param name="ImageInstructions">The list of rules for mapping Excel columns to image shapes.</param>
public record MapNode(
    IReadOnlySet<SheetIdentifier> Sheets,
    SlideIdentifier Slide,
    IReadOnlyList<TextInstruction> TextInstructions,
    IReadOnlyList<ImageInstruction> ImageInstructions);

/// <summary>
///     Represents the complete configuration for a generation job.
///     A recipe consists of multiple mapping nodes that define how various data sources are merged into slides.
/// </summary>
/// <param name="Nodes">The list of mapping nodes that form the recipe.</param>
/// <param name="Name">The display name of the recipe.</param>
public record Recipe(IReadOnlyList<MapNode> Nodes, string Name = "Default Recipe");
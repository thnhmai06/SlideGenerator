/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Recipe
 * File: TextInstruction.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;

namespace SlideGenerator.Recipe.Domain.Models.Components;

/// <summary>
///     Defines a mapping between one or more Excel columns and one or more text placeholders in a slide.
/// </summary>
/// <param name="Placeholders">The set of placeholder tags (e.g., "{{Name}}") to be replaced.</param>
/// <param name="Columns">The list of Excel columns whose values will provide the replacement text.</param>
public record TextInstruction(
    IReadOnlySet<string> Placeholders,
    IReadOnlyList<ColumnIdentifier> Columns);
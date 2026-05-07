/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
 * File: TextInstruction.cs
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

using SlideGenerator.Document.Sheet.Models;

namespace SlideGenerator.Pipeline.Generating.Models;

/// <summary>
///     Defines a mapping between one or more Excel columns and one or more text placeholders in a slide.
/// </summary>
/// <param name="Placeholders">The set of placeholder tags (e.g., "{{Name}}") to be replaced.</param>
/// <param name="Columns">The list of Excel columns whose values will provide the replacement text.</param>
public record TextInstruction(
    IReadOnlySet<string> Placeholders,
    IReadOnlyList<ColumnIdentifier> Columns);
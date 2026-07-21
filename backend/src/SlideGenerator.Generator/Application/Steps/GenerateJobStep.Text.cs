/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GenerateJobStep.Text.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Recipe.Domain.Models.Components;

namespace SlideGenerator.Generator.Application.Steps;

public sealed partial class GenerateJobStep
{
    /// <summary>
    ///     Reads a single data row and builds a placeholder → value dictionary for template text substitution.
    ///     For each <see cref="TextInstruction" /> the first non-empty cell value across its column list wins;
    ///     all placeholders in that instruction are mapped to that value (or <c>""</c> if all columns are empty).
    /// </summary>
    /// <param name="rowValues">Column-identifier-to-cell-value map for the data row.</param>
    /// <param name="instructions">Text instructions from the recipe map node.</param>
    /// <returns>Dictionary keyed by placeholder name, value is the resolved cell text.</returns>
    internal static IReadOnlyDictionary<string, string> BuildRowTextValues(
        IReadOnlyDictionary<ColumnIdentifier, string> rowValues,
        IReadOnlyList<TextInstruction> instructions)
    {
        var result = new Dictionary<string, string>();

        foreach (var instruction in instructions)
        {
            var value = instruction.Columns
                .Select(col => rowValues.GetValueOrDefault(col))
                .FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";

            foreach (var placeholder in instruction.Placeholders)
                result[placeholder] = value;
        }

        return result;
    }
}

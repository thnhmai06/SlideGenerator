/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: BindingCandidate.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Desktop.Features.RecipeEditor.Models;

/// <summary>How confidently <see cref="BindingMatcher" /> matched a placeholder to a column.</summary>
public enum BindingConfidence
{
    /// <summary>Placeholder name equals a column name literally.</summary>
    Exact,

    /// <summary>Placeholder and column names are equal after loose normalization.</summary>
    Normalized,

    /// <summary>Multiple columns qualify, or only a partial (substring) match exists — not auto-assigned.</summary>
    Ambiguous,

    /// <summary>No column qualifies at any level.</summary>
    None
}

/// <summary>
///     One placeholder's auto-bind suggestion. <see cref="Column" /> is set only for
///     <see cref="BindingConfidence.Exact" />/<see cref="BindingConfidence.Normalized" /> (a single column
///     the UI can assign automatically); <see cref="Candidates" /> is the filtered dropdown list for
///     <see cref="BindingConfidence.Ambiguous" /> (empty otherwise).
/// </summary>
public sealed record BindingCandidate(
    string Placeholder,
    BindingConfidence Confidence,
    string? Column,
    IReadOnlyList<string> Candidates);

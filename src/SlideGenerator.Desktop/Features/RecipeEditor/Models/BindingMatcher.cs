/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: BindingMatcher.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Utilities;

namespace SlideGenerator.Desktop.Features.RecipeEditor.Models;

/// <summary>
///     Pure auto-bind suggestion logic — no I/O, no Syncfusion. Suggests, never assigns silently: a
///     placeholder is only auto-assigned at <see cref="BindingConfidence.Exact" />/
///     <see cref="BindingConfidence.Normalized" /> when exactly one column qualifies; anything looser or more
///     ambiguous is left for the user to decide (plan §5.2 "Auto-bind" table).
/// </summary>
public static class BindingMatcher
{
    /// <summary>Suggests a binding for each placeholder against the given column names.</summary>
    /// <param name="placeholders">Template placeholder tags (e.g. "Name" from "{{Name}}").</param>
    /// <param name="columns">Worksheet column header names.</param>
    public static IReadOnlyList<BindingCandidate> Match(IReadOnlyList<string> placeholders, IReadOnlyList<string> columns)
    {
        return placeholders.Select(placeholder => MatchOne(placeholder, columns)).ToList();
    }

    private static BindingCandidate MatchOne(string placeholder, IReadOnlyList<string> columns)
    {
        var normalizedPlaceholder = TextNormalization.NormalizeForMatching(placeholder);

        var topTier = columns.Where(c => TextNormalization.NormalizeForMatching(c) == normalizedPlaceholder).ToList();
        if (topTier.Count == 1)
        {
            var column = topTier[0];
            var confidence = column == placeholder ? BindingConfidence.Exact : BindingConfidence.Normalized;
            return new BindingCandidate(placeholder, confidence, column, []);
        }

        if (topTier.Count >= 2)
            return new BindingCandidate(placeholder, BindingConfidence.Ambiguous, null, topTier);

        var partial = columns.Where(c => IsPartialMatch(TextNormalization.NormalizeForMatching(c), normalizedPlaceholder)).ToList();
        return partial.Count > 0
            ? new BindingCandidate(placeholder, BindingConfidence.Ambiguous, null, partial)
            : new BindingCandidate(placeholder, BindingConfidence.None, null, []);
    }

    private static bool IsPartialMatch(string normalizedColumn, string normalizedPlaceholder)
    {
        if (normalizedColumn.Length == 0 || normalizedPlaceholder.Length == 0) return false;
        return normalizedColumn.Contains(normalizedPlaceholder, StringComparison.Ordinal) ||
               normalizedPlaceholder.Contains(normalizedColumn, StringComparison.Ordinal);
    }
}

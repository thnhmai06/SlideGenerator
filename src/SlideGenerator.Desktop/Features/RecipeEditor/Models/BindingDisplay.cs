/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: BindingDisplay.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Desktop.Features.RecipeEditor.Models;

/// <summary>
///     The four states plan §5.2 defines for both text-placeholder bindings and canvas image-shape overlays —
///     one shared model so the count summary ("12 đã ghép · 3 là đề xuất · 2 cần bạn chọn · 1 chưa gán") and
///     the styling hooks (<c>assigned</c>/<c>suggested</c>/<c>needs-selection</c>/<c>unassigned</c> in
///     Controls.axaml) mean the same thing everywhere they appear.
/// </summary>
public enum BindingDisplayState
{
    /// <summary>Column set — either a real saved binding, or an Exact/confirmed auto-bind match.</summary>
    Assigned,

    /// <summary>Auto-bound at Normalized confidence but not yet confirmed by the user (plan §5.2 table).</summary>
    Suggested,

    /// <summary>Ambiguous — multiple or partial candidates; the user must pick from <see cref="BindingDisplay.Candidates" />.</summary>
    NeedsSelection,

    /// <summary>No candidate at all.</summary>
    Unassigned
}

/// <summary>One name's (placeholder or shape) resolved display state, column (if any), and dropdown candidates.</summary>
public sealed record BindingDisplay(string Name, BindingDisplayState State, string? Column, IReadOnlyList<string> Candidates);

/// <summary>
///     Resolves a name (placeholder or shape) to its <see cref="BindingDisplay" /> against a real, already-saved
///     binding (if any) and the auto-bind suggestion from <see cref="BindingMatcher" /> otherwise — and folds a
///     set of them into the count summary shown in Guided step ④ and the Advanced warning strip.
/// </summary>
public static class BindingDisplayResolver
{
    /// <summary>
    ///     Resolves one name. A non-null <paramref name="existingColumn" /> (a binding already saved into the
    ///     recipe) always wins as <see cref="BindingDisplayState.Assigned" />, regardless of what the matcher
    ///     would suggest. Otherwise falls back to <see cref="BindingMatcher" />'s per-tier suggestion:
    ///     Exact assigns silently, Normalized assigns but stays <see cref="BindingDisplayState.Suggested" />
    ///     until <paramref name="touched" />, Ambiguous/None never auto-assign.
    /// </summary>
    public static BindingDisplay Resolve(string name, string? existingColumn, IReadOnlyList<string> availableColumns, bool touched)
    {
        if (existingColumn is not null)
            return new BindingDisplay(name, BindingDisplayState.Assigned, existingColumn, []);

        var candidate = BindingMatcher.Match([name], availableColumns)[0];
        return candidate.Confidence switch
        {
            BindingConfidence.Exact => new BindingDisplay(name, BindingDisplayState.Assigned, candidate.Column, []),
            BindingConfidence.Normalized => new BindingDisplay(
                name, touched ? BindingDisplayState.Assigned : BindingDisplayState.Suggested, candidate.Column, []),
            BindingConfidence.Ambiguous => new BindingDisplay(name, BindingDisplayState.NeedsSelection, null, candidate.Candidates),
            _ => new BindingDisplay(name, BindingDisplayState.Unassigned, null, [])
        };
    }

    /// <summary>Counts each state across a set of resolved bindings, for the "X đã ghép · Y là đề xuất · …" summary.</summary>
    public static (int Assigned, int Suggested, int NeedsSelection, int Unassigned) Summarize(IReadOnlyList<BindingDisplay> displays)
    {
        return (
            displays.Count(d => d.State == BindingDisplayState.Assigned),
            displays.Count(d => d.State == BindingDisplayState.Suggested),
            displays.Count(d => d.State == BindingDisplayState.NeedsSelection),
            displays.Count(d => d.State == BindingDisplayState.Unassigned));
    }
}

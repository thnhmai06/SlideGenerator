/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: BindingDisplayResolverTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="BindingDisplayResolver" /> — the shared four-state derivation used by both
///     text-placeholder bindings and canvas image-shape overlays (plan §5.2).
/// </summary>
public sealed class BindingDisplayResolverTests
{
    [Fact]
    public void Resolve_ExistingColumn_IsAssignedRegardlessOfMatcher()
    {
        var result = BindingDisplayResolver.Resolve("Note", "Unrelated", ["Note"], touched: false);

        result.State.Should().Be(BindingDisplayState.Assigned);
        result.Column.Should().Be("Unrelated");
    }

    [Fact]
    public void Resolve_ExactMatchNoExisting_IsAssignedSilently()
    {
        var result = BindingDisplayResolver.Resolve("Name", null, ["Name"], touched: false);

        result.State.Should().Be(BindingDisplayState.Assigned);
        result.Column.Should().Be("Name");
    }

    [Fact]
    public void Resolve_NormalizedMatchNotTouched_IsSuggested()
    {
        var result = BindingDisplayResolver.Resolve("HoTen", null, ["Họ tên"], touched: false);

        result.State.Should().Be(BindingDisplayState.Suggested);
        result.Column.Should().Be("Họ tên");
    }

    [Fact]
    public void Resolve_NormalizedMatchTouched_IsAssigned()
    {
        var result = BindingDisplayResolver.Resolve("HoTen", null, ["Họ tên"], touched: true);

        result.State.Should().Be(BindingDisplayState.Assigned);
        result.Column.Should().Be("Họ tên");
    }

    [Fact]
    public void Resolve_AmbiguousMatch_IsNeedsSelectionWithNoColumnButCandidates()
    {
        var result = BindingDisplayResolver.Resolve("Name", null, ["Name", "name"], touched: false);

        result.State.Should().Be(BindingDisplayState.NeedsSelection);
        result.Column.Should().BeNull();
        result.Candidates.Should().BeEquivalentTo(["Name", "name"]);
    }

    [Fact]
    public void Resolve_NoCandidate_IsUnassigned()
    {
        var result = BindingDisplayResolver.Resolve("Note", null, ["Name"], touched: false);

        result.State.Should().Be(BindingDisplayState.Unassigned);
        result.Column.Should().BeNull();
    }

    [Fact]
    public void Summarize_MixedStates_CountsEachStateCorrectly()
    {
        var displays = new[]
        {
            new BindingDisplay("A", BindingDisplayState.Assigned, "A", []),
            new BindingDisplay("B", BindingDisplayState.Assigned, "B", []),
            new BindingDisplay("C", BindingDisplayState.Suggested, "C", []),
            new BindingDisplay("D", BindingDisplayState.NeedsSelection, null, ["D1", "D2"]),
            new BindingDisplay("E", BindingDisplayState.Unassigned, null, [])
        };

        var (assigned, suggested, needsSelection, unassigned) = BindingDisplayResolver.Summarize(displays);

        assigned.Should().Be(2);
        suggested.Should().Be(1);
        needsSelection.Should().Be(1);
        unassigned.Should().Be(1);
    }
}

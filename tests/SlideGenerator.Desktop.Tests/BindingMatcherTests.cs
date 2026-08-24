/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: BindingMatcherTests.cs
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
///     Unit tests for <see cref="BindingMatcher" />'s four-tier auto-bind suggestion logic (plan §5.2).
/// </summary>
public sealed class BindingMatcherTests
{
    /// <summary>A placeholder whose name equals a column name literally must be Exact.</summary>
    [Fact]
    public void Match_LiteralNameMatch_ReturnsExact()
    {
        var result = BindingMatcher.Match(["Name"], ["Name", "Department"]);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new BindingCandidate("Name", BindingConfidence.Exact, "Name", []));
    }

    /// <summary>"HoTen" must match "Họ tên" after diacritics/case/whitespace normalization.</summary>
    [Fact]
    public void Match_DiacriticsCaseWhitespaceDiffer_ReturnsNormalized()
    {
        var result = BindingMatcher.Match(["HoTen"], ["Họ tên", "Phòng ban"]);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new BindingCandidate("HoTen", BindingConfidence.Normalized, "Họ tên", []));
    }

    /// <summary>Two columns normalizing to the same text must not be auto-assigned — Ambiguous instead.</summary>
    [Fact]
    public void Match_TwoColumnsNormalizeTheSame_ReturnsAmbiguousWithNoAssignedColumn()
    {
        var result = BindingMatcher.Match(["Name"], ["Name", "name"]);

        var candidate = result.Should().ContainSingle().Subject;
        candidate.Confidence.Should().Be(BindingConfidence.Ambiguous);
        candidate.Column.Should().BeNull();
        candidate.Candidates.Should().BeEquivalentTo(["Name", "name"]);
    }

    /// <summary>A substring-only match (e.g. "Name" inside "FullName") is Ambiguous, never auto-assigned.</summary>
    [Fact]
    public void Match_PartialSubstringMatch_ReturnsAmbiguous()
    {
        var result = BindingMatcher.Match(["Name"], ["FullName"]);

        var candidate = result.Should().ContainSingle().Subject;
        candidate.Confidence.Should().Be(BindingConfidence.Ambiguous);
        candidate.Column.Should().BeNull();
        candidate.Candidates.Should().BeEquivalentTo(["FullName"]);
    }

    /// <summary>No candidate column at all must be None with no assigned column and no dropdown candidates.</summary>
    [Fact]
    public void Match_NoCandidateColumn_ReturnsNone()
    {
        var result = BindingMatcher.Match(["Note"], ["Name", "Department"]);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new BindingCandidate("Note", BindingConfidence.None, null, []));
    }
}

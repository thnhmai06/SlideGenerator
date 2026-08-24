/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: TextBindingsViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>Unit tests for <see cref="TextBindingsViewModel" /> — placeholder-row derivation, touch tracking, and save projection.</summary>
public sealed class TextBindingsViewModelTests
{
    [Fact]
    public void Load_ExistingInstruction_RowIsAssignedWithSavedColumn()
    {
        var instructions = new List<TextInstruction> { new(new HashSet<string> { "Name" }, [new ColumnIdentifier("FullName")]) };
        var vm = new TextBindingsViewModel();

        vm.Load(["Name"], instructions, ["FullName"]);

        var row = vm.Rows.Should().ContainSingle().Subject;
        row.Binding.State.Should().Be(BindingDisplayState.Assigned);
        row.Binding.Column.Should().Be("FullName");
    }

    [Fact]
    public void Load_NoExistingInstructionNormalizedMatch_RowIsSuggested()
    {
        var vm = new TextBindingsViewModel();

        vm.Load(["HoTen"], [], ["Họ tên"]);

        vm.Rows.Should().ContainSingle().Which.Binding.State.Should().Be(BindingDisplayState.Suggested);
    }

    [Fact]
    public void SetColumn_MarksAssignedAndSurvivesReload_AsAssignedNotSuggested()
    {
        var vm = new TextBindingsViewModel();
        vm.Load(["HoTen"], [], ["Họ tên"]);

        vm.SetColumn(vm.Rows[0], "Họ tên");

        vm.Rows[0].Binding.State.Should().Be(BindingDisplayState.Assigned);
        vm.Rows[0].Binding.Column.Should().Be("Họ tên");
    }

    [Fact]
    public void Summary_MixedRows_CountsMatchBindingDisplayResolverSummarize()
    {
        var vm = new TextBindingsViewModel();
        vm.Load(["Exact", "Ambiguous", "None"], [], ["Exact", "Ambiguous", "ambiguous"]);

        var summary = vm.Summary;

        summary.Assigned.Should().Be(1);
        summary.NeedsSelection.Should().Be(1);
        summary.Unassigned.Should().Be(1);
    }

    [Fact]
    public void ToTextInstructions_OnlyIncludesRowsWithAColumn()
    {
        var vm = new TextBindingsViewModel();
        vm.Load(["Assigned", "Unassigned"], [], ["Assigned"]);

        var result = vm.ToTextInstructions();

        result.Should().ContainSingle();
        result[0].Placeholders.Should().BeEquivalentTo(["Assigned"]);
        result[0].Columns.Should().BeEquivalentTo([new ColumnIdentifier("Assigned")]);
    }
}

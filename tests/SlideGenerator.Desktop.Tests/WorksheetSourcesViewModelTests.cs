/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: WorksheetSourcesViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Summarizer.Workbooks;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>Unit tests for <see cref="WorksheetSourcesViewModel" />'s pure Load/Remove/save-projection paths (no file picker/I/O).</summary>
public sealed class WorksheetSourcesViewModelTests
{
    private static readonly WorkbookIdentifier Workbook = new("data.xlsx");
    private static readonly WorksheetIdentifier Worksheet = new("Sheet1");

    private static WorksheetSourcesViewModel CreateViewModel()
    {
        return new WorksheetSourcesViewModel(Substitute.For<IFilePicker>(), Substitute.For<ISummaryCache>());
    }

    [Fact]
    public void Load_OnePair_AddsOneRow()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 5, new WorksheetPreview(["A"], []));
        var source = new WorksheetSource(Workbook, Worksheet);
        var vm = CreateViewModel();

        vm.Load([(source, summary)]);

        vm.Sources.Should().ContainSingle();
    }

    [Fact]
    public void Load_ReplacesPreviousRows()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 5, new WorksheetPreview(["A"], []));
        var source = new WorksheetSource(Workbook, Worksheet);
        var vm = CreateViewModel();
        vm.Load([(source, summary)]);

        vm.Load([]);

        vm.Sources.Should().BeEmpty();
    }

    [Fact]
    public void Remove_RemovesTheGivenRow()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 5, new WorksheetPreview(["A"], []));
        var source = new WorksheetSource(Workbook, Worksheet);
        var vm = CreateViewModel();
        vm.Load([(source, summary)]);

        vm.RemoveCommand.Execute(vm.Sources[0]);

        vm.Sources.Should().BeEmpty();
    }

    [Fact]
    public void ToWorksheetSources_ProjectsEachRow()
    {
        var summary = new WorksheetSummary(Workbook, Worksheet, 5, new WorksheetPreview(["A"], []));
        var source = new WorksheetSource(Workbook, Worksheet);
        var vm = CreateViewModel();
        vm.Load([(source, summary)]);

        var result = vm.ToWorksheetSources();

        result.Should().ContainSingle();
        result[0].Workbook.Should().Be(Workbook);
        result[0].Worksheet.Should().Be(Worksheet);
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: RecipesViewModelTests.cs
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
using SlideGenerator.Desktop.Features.Recipes.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Summarizer.Workbooks;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="RecipesViewModel" />'s confirm-on-leave behavior (plan §5.2: "Rời editor lúc
///     dirty → dialog xác nhận") — <see cref="RecipesViewModel.CloseEditorCommand" /> must prompt only when the
///     open editor has unsaved edits, and must respect the user's answer.
/// </summary>
public sealed class RecipesViewModelTests
{
    private static RecipesViewModel CreateViewModel(IDialogService? dialogService = null)
    {
        var repository = Substitute.For<IRecipeRepository>();
        repository.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<IRecipeMetadata>());
        return new RecipesViewModel(repository, Substitute.For<IRecipePackageService>(), Substitute.For<IService>(),
            dialogService ?? Substitute.For<IDialogService>(), Substitute.For<IFilePicker>(), Substitute.For<IServiceProvider>(),
            Substitute.For<ISummaryCache>());
    }

    private static RecipesViewModel CreateViewModel(IRecipeRepository repository, ISummaryCache summaryCache)
    {
        return new RecipesViewModel(repository, Substitute.For<IRecipePackageService>(), Substitute.For<IService>(),
            Substitute.For<IDialogService>(), Substitute.For<IFilePicker>(), Substitute.For<IServiceProvider>(),
            summaryCache);
    }

    private static RecipeEditorViewModel CreateEditor(bool isDirty)
    {
        var editor = new RecipeEditorViewModel(Substitute.For<ISummaryCache>(), Substitute.For<IFilePicker>(),
            Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        editor.IsDirty = isDirty;
        return editor;
    }

    [Fact]
    public async Task CloseEditorCommand_EditorDirtyAndConfirmed_ClosesEditor()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var vm = CreateViewModel(dialogService);
        vm.Editor = CreateEditor(isDirty: true);

        await vm.CloseEditorCommand.ExecuteAsync(null);

        vm.Editor.Should().BeNull();
        vm.IsEditorOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CloseEditorCommand_EditorDirtyAndCancelled_StaysOpen()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var vm = CreateViewModel(dialogService);
        var editor = CreateEditor(isDirty: true);
        vm.Editor = editor;

        await vm.CloseEditorCommand.ExecuteAsync(null);

        vm.Editor.Should().BeSameAs(editor);
    }

    [Fact]
    public async Task CloseEditorCommand_EditorNotDirty_ClosesWithoutPrompting()
    {
        var dialogService = Substitute.For<IDialogService>();
        var vm = CreateViewModel(dialogService);
        vm.Editor = CreateEditor(isDirty: false);

        await vm.CloseEditorCommand.ExecuteAsync(null);

        vm.Editor.Should().BeNull();
        await dialogService.DidNotReceive().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private static RecipeEntry MakeEntry(int id, int textInstructionCount, int imageInstructionCount, params WorkbookIdentifier[] sourceWorkbooks)
    {
        var mapping = new Mapping(
            sourceWorkbooks.Select(wb => new WorksheetSource(wb, new WorksheetIdentifier("Sheet1"))).ToList(),
            new PresentationSource(new PresentationIdentifier("template.pptx"), new SlideIdentifier(0)),
            Enumerable.Range(0, textInstructionCount)
                .Select(_ => new TextInstruction(new HashSet<string> { "{{x}}" }, [new ColumnIdentifier("A")]))
                .ToList(),
            Enumerable.Range(0, imageInstructionCount)
                .Select(_ => new ImageInstruction(new HashSet<ShapeIdentifier> { new("Picture 1") }, [new ColumnIdentifier("B")],
                    new ImageEditInstruction([])))
                .ToList());
        return new RecipeEntry(id, "Test recipe", new SlideGenerator.Recipe.Models.Recipe([mapping]), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(5);
        condition().Should().BeTrue("condition did not become true within the timeout");
    }

    [Fact]
    public async Task SelectedRecipe_StatsAggregateAcrossMappingsAndSources_ComputesCounts()
    {
        var repository = Substitute.For<IRecipeRepository>();
        var summaryCache = Substitute.For<ISummaryCache>();
        var workbookA = new WorkbookIdentifier("a.xlsx");
        var workbookB = new WorkbookIdentifier("b.xlsx");
        var entry = MakeEntry(5, textInstructionCount: 3, imageInstructionCount: 2, workbookA, workbookB);
        repository.GetAsync(5, Arg.Any<CancellationToken>()).Returns(entry);
        summaryCache.GetWorkbookAsync(workbookA, false, Arg.Any<CancellationToken>())
            .Returns(new WorkbookSummary("a.xlsx", "a", [new WorksheetSummary(workbookA, new WorksheetIdentifier("Sheet1"), 10)]));
        summaryCache.GetWorkbookAsync(workbookB, false, Arg.Any<CancellationToken>())
            .Returns(new WorkbookSummary("b.xlsx", "b", [new WorksheetSummary(workbookB, new WorksheetIdentifier("Sheet1"), 20)]));
        var vm = CreateViewModel(repository, summaryCache);

        vm.SelectedRecipe = new RecipeListItemViewModel(entry, repository, Substitute.For<IRecipePackageService>(),
            Substitute.For<IDialogService>(), Substitute.For<IFilePicker>());
        await WaitUntilAsync(() => !vm.IsLoadingStats);

        vm.MappingCount.Should().Be(1);
        vm.SourceCount.Should().Be(2);
        vm.TextInstructionCount.Should().Be(3);
        vm.ImageInstructionCount.Should().Be(2);
        vm.RecordCount.Should().Be(30);
        vm.RecordCountDisplay.Should().Be("~30");
    }

    [Fact]
    public async Task SelectedRecipe_SummaryCacheThrows_RecordCountFallsBackToDash()
    {
        var repository = Substitute.For<IRecipeRepository>();
        var summaryCache = Substitute.For<ISummaryCache>();
        var workbookA = new WorkbookIdentifier("a.xlsx");
        var entry = MakeEntry(7, textInstructionCount: 1, imageInstructionCount: 0, workbookA);
        repository.GetAsync(7, Arg.Any<CancellationToken>()).Returns(entry);
        summaryCache.GetWorkbookAsync(workbookA, false, Arg.Any<CancellationToken>())
            .Returns<Task<WorkbookSummary>>(_ => throw new InvalidOperationException("workbook locked"));
        var vm = CreateViewModel(repository, summaryCache);

        vm.SelectedRecipe = new RecipeListItemViewModel(entry, repository, Substitute.For<IRecipePackageService>(),
            Substitute.For<IDialogService>(), Substitute.For<IFilePicker>());
        await WaitUntilAsync(() => !vm.IsLoadingStats);

        vm.RecordCount.Should().BeNull();
        vm.RecordCountDisplay.Should().Be("—");
        vm.MappingCount.Should().Be(1); // set before the failing summary loop — a count failure must not hide these
    }
}

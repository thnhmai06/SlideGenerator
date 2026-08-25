/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: RecipeEditorViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Avalonia.Platform.Storage;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.RecipeEditor.Models;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Summarizer.Presentations;
using SlideGenerator.Summarizer.Workbooks;
using Xunit;
using RecipeModel = SlideGenerator.Recipe.Models.Recipe;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="RecipeEditorViewModel" />'s coordination of the canvas/text-bindings/sources
///     panels — column flattening across worksheets, touched-state surviving a mapping switch, and edits being
///     projected back onto the mapping before navigating away.
/// </summary>
public sealed class RecipeEditorViewModelTests
{
    private static readonly WorkbookIdentifier Workbook = new("book.xlsx");
    private static readonly WorksheetIdentifier Worksheet = new("Sheet1");
    private static readonly PresentationIdentifier Presentation = new("template.pptx");

    private static Mapping CreateMapping(SlideIdentifier slide, IReadOnlyList<TextInstruction>? textInstructions = null)
    {
        return new Mapping(
            [new WorksheetSource(Workbook, Worksheet)],
            new PresentationSource(Presentation, slide),
            textInstructions ?? [],
            []);
    }

    private static ISummaryCache CreateSummaryCache(IReadOnlyList<string> placeholders, IReadOnlyList<string> headers)
    {
        var cache = Substitute.For<ISummaryCache>();
        var slide1 = new SlideSummary(Presentation, new SlideIdentifier(1), placeholders, [], null, new SizeF(100, 100));
        var slide2 = new SlideSummary(Presentation, new SlideIdentifier(2), placeholders, [], null, new SizeF(100, 100));
        cache.GetPresentationAsync(Presentation, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new PresentationSummary(Presentation.PresentationPath, [slide1, slide2]));

        var worksheetSummary = new WorksheetSummary(Workbook, Worksheet, headers.Count, new WorksheetPreview(headers, []));
        cache.GetWorkbookAsync(Workbook, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new WorkbookSummary(Workbook.BookPath, "book", [worksheetSummary]));

        return cache;
    }

    [Fact]
    public void FlattenAvailableColumns_MultipleWorksheets_UnionsAndDedupesHeaders()
    {
        var summaries = new[]
        {
            new WorksheetSummary(Workbook, Worksheet, 1, new WorksheetPreview(["Name", "Photo"], [])),
            new WorksheetSummary(Workbook, new WorksheetIdentifier("Sheet2"), 1, new WorksheetPreview(["Photo", "Bio"], []))
        };

        var columns = RecipeEditorViewModel.FlattenAvailableColumns(summaries);

        columns.Should().BeEquivalentTo(["Name", "Photo", "Bio"]);
    }

    [Fact]
    public async Task InitializeAsync_SingleMapping_HidesNavigatorAndLoadsPanels()
    {
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1))]);

        await vm.InitializeAsync(recipe);

        vm.ShowMappingNavigator.Should().BeFalse();
        vm.SelectedSession.Should().NotBeNull();
        vm.TextBindings.Rows.Should().ContainSingle(r => r.Placeholder == "name");
    }

    [Fact]
    public async Task InitializeAsync_TwoMappings_ShowsNavigator()
    {
        var cache = CreateSummaryCache([], []);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(2))]);

        await vm.InitializeAsync(recipe);

        vm.ShowMappingNavigator.Should().BeTrue();
        vm.Sessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task SelectSessionAsync_SwitchAwayAndBack_TouchedColumnStaysAssignedNotSuggested()
    {
        // "Name" auto-binds as Suggested (normalized match) until touched; confirming it via SetColumn must
        // survive switching to the other mapping and back, or the user's confirmation silently reverts.
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(2))]);
        await vm.InitializeAsync(recipe);

        var row = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        row.Binding.State.Should().Be(BindingDisplayState.Suggested);
        vm.TextBindings.SetColumn(row, "Name");

        await vm.SelectSessionAsync(vm.Sessions[1]);
        await vm.SelectSessionAsync(vm.Sessions[0]);

        var reloadedRow = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        reloadedRow.Binding.State.Should().Be(BindingDisplayState.Assigned);
        reloadedRow.Binding.Column.Should().Be("Name");
    }

    [Fact]
    public async Task SelectSessionAsync_SwitchingAway_ProjectsEditsOntoPreviousMapping()
    {
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(2))]);
        await vm.InitializeAsync(recipe);

        var row = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        vm.TextBindings.SetColumn(row, "Name");

        await vm.SelectSessionAsync(vm.Sessions[1]);

        vm.Sessions[0].Mapping.TextInstructions.Should().ContainSingle(
            i => i.Placeholders.Contains("name") && i.Columns.Single().ColumnName == "Name");
    }

    [Fact]
    public async Task SelectSessionAsync_TargetSlideMissing_DoesNotProjectStaleContentOntoNewSession()
    {
        // Session B's template slide (99) doesn't exist in the fake presentation (only 1 and 2 do), so
        // LoadMappingAsync bails before touching the panels — they still show A's content. Without the
        // _loadedSession guard, the next SelectSessionAsync would wrongly project A's edited text binding
        // onto B's mapping.
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(99))]);
        await vm.InitializeAsync(recipe);

        var row = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        vm.TextBindings.SetColumn(row, "Name");

        await vm.SelectSessionAsync(vm.Sessions[1]);

        vm.Sessions[1].Mapping.TextInstructions.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectSessionAsync_JustSwitchingMappings_DoesNotMarkDirty()
    {
        var cache = CreateSummaryCache([], []);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(2))]);
        await vm.InitializeAsync(recipe);

        await vm.SelectSessionAsync(vm.Sessions[1]);

        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task ToRecipe_ReflectsProjectedEditsAcrossAllSessions()
    {
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1))]);
        await vm.InitializeAsync(recipe);

        var row = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        vm.TextBindings.SetColumn(row, "Name");

        var result = vm.ToRecipe();

        result.Mappings.Should().ContainSingle().Which.TextInstructions.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveCommand_InitiallyNotDirty_CannotExecute()
    {
        var vm = new RecipeEditorViewModel(CreateSummaryCache([], []), Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        await vm.InitializeAsync(new RecipeModel([]));

        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_DirtyWithName_CanExecute()
    {
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1))]);
        await vm.InitializeAsync(recipe);
        vm.Name = "Recipe A";

        var row = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        vm.TextBindings.SetColumn(row, "Name");

        vm.IsDirty.Should().BeTrue();
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SaveCommand_BlankName_CannotExecute()
    {
        var cache = CreateSummaryCache(["name"], ["Name"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1))]);
        await vm.InitializeAsync(recipe);

        var row = vm.TextBindings.Rows.Should().ContainSingle().Subject;
        vm.TextBindings.SetColumn(row, "Name");

        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_UnresolvedAmbiguousBinding_CannotExecute()
    {
        // "Name" placeholder against a "FullName" column is a partial-match Ambiguous binding — never
        // auto-assigned, so it must block Save until the user picks a candidate.
        var cache = CreateSummaryCache(["Name"], ["FullName"]);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1))]);
        await vm.InitializeAsync(recipe);
        vm.Name = "Recipe A";

        vm.HasUnresolvedBindings.Should().BeTrue();
        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_NewRecipe_CallsAddAndAdoptsReturnedId()
    {
        var repository = Substitute.For<IRecipeRepository>();
        var savedMetadata = new RecipeEntry(42, "Recipe A", new RecipeModel([]), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        repository.AddAsync(Arg.Any<RecipeInput>()).Returns(savedMetadata);

        var vm = new RecipeEditorViewModel(CreateSummaryCache([], []), Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), repository);
        await vm.InitializeAsync(new RecipeModel([]));
        vm.Name = "Recipe A";

        var saved = false;
        vm.Saved += () => saved = true;
        await vm.SaveCommand.ExecuteAsync(null);

        vm.Id.Should().Be(42);
        vm.IsDirty.Should().BeFalse();
        saved.Should().BeTrue();
        await repository.Received(1).AddAsync(Arg.Any<RecipeInput>());
    }

    [Fact]
    public async Task SaveAsync_ExistingRecipe_CallsUpdateNotAdd()
    {
        var repository = Substitute.For<IRecipeRepository>();
        var savedMetadata = new RecipeEntry(7, "Recipe A v2", new RecipeModel([]), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        repository.UpdateAsync(7, Arg.Any<RecipeInput>()).Returns(savedMetadata);

        var vm = new RecipeEditorViewModel(CreateSummaryCache([], []), Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), repository);
        await vm.InitializeAsync(7, "Recipe A", new RecipeModel([]));
        vm.Name = "Recipe A v2";

        await vm.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).UpdateAsync(7, Arg.Any<RecipeInput>());
        await repository.DidNotReceive().AddAsync(Arg.Any<RecipeInput>());
    }

    [Fact]
    public async Task AddMappingCommand_TemplatePicked_AppendsAndSelectsNewMapping()
    {
        var newPresentation = new PresentationIdentifier("new.pptx");
        var newSlide = new SlideIdentifier(1);
        var template = new PresentationSource(newPresentation, newSlide);

        var cache = CreateSummaryCache([], []);
        var newSlideSummary = new SlideSummary(newPresentation, newSlide, [], [], null, new SizeF(100, 100));
        cache.GetPresentationAsync(newPresentation, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new PresentationSummary(newPresentation.PresentationPath, [newSlideSummary]));

        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowTemplatePickerAsync().Returns(template);

        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), dialogService, Substitute.For<IRecipeRepository>());
        await vm.InitializeAsync(new RecipeModel([]));

        await vm.AddMappingCommand.ExecuteAsync(null);

        var session = vm.Sessions.Should().ContainSingle().Subject;
        vm.SelectedSession.Should().BeSameAs(session);
        session.Mapping.Template.Should().Be(template);
    }

    [Fact]
    public async Task AddMappingCommand_DialogCancelled_DoesNotAddMapping()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowTemplatePickerAsync().Returns((PresentationSource?)null);
        var vm = new RecipeEditorViewModel(CreateSummaryCache([], []), Substitute.For<IFilePicker>(), dialogService, Substitute.For<IRecipeRepository>());
        await vm.InitializeAsync(new RecipeModel([]));

        await vm.AddMappingCommand.ExecuteAsync(null);

        vm.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveMappingCommand_RemovesSessionAndSelectsNeighbor()
    {
        var cache = CreateSummaryCache([], []);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(2))]);
        await vm.InitializeAsync(recipe);

        var removed = vm.Sessions[0];
        vm.RemoveMappingCommand.Execute(removed);

        vm.Sessions.Should().ContainSingle();
        vm.Sessions.Should().NotContain(removed);
    }

    [Fact]
    public async Task MoveMappingDownCommand_SwapsOrder()
    {
        var cache = CreateSummaryCache([], []);
        var vm = new RecipeEditorViewModel(cache, Substitute.For<IFilePicker>(), Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var recipe = new RecipeModel([CreateMapping(new SlideIdentifier(1)), CreateMapping(new SlideIdentifier(2))]);
        await vm.InitializeAsync(recipe);

        var first = vm.Sessions[0];
        vm.MoveMappingDownCommand.Execute(first);

        vm.Sessions[1].Should().BeSameAs(first);
    }

    [Fact]
    public async Task PickFallbackImageCommand_PathChosen_SetsOverlayFallbackImagePath()
    {
        var picker = Substitute.For<IFilePicker>();
        picker.PickFileAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<FilePickerFileType>?>()).Returns("chosen.png");
        var vm = new RecipeEditorViewModel(CreateSummaryCache([], []), picker, Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var shape = new ShapeSummary(new SlideIdentifier(1), new ShapeIdentifier("Avatar"), new RectangleF(0, 0, 10, 10));
        var overlay = new ShapeOverlayViewModel(shape, new BindingDisplay("Avatar", BindingDisplayState.Unassigned, null, []),
            new ImageEditInstruction([]), null, []);

        await vm.PickFallbackImageCommand.ExecuteAsync(overlay);

        overlay.FallbackImagePath.Should().Be("chosen.png");
    }

    [Fact]
    public async Task PickFallbackImageCommand_Cancelled_LeavesFallbackImagePathUnchanged()
    {
        var picker = Substitute.For<IFilePicker>();
        picker.PickFileAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<FilePickerFileType>?>()).Returns((string?)null);
        var vm = new RecipeEditorViewModel(CreateSummaryCache([], []), picker, Substitute.For<IDialogService>(), Substitute.For<IRecipeRepository>());
        var shape = new ShapeSummary(new SlideIdentifier(1), new ShapeIdentifier("Avatar"), new RectangleF(0, 0, 10, 10));
        var overlay = new ShapeOverlayViewModel(shape, new BindingDisplay("Avatar", BindingDisplayState.Unassigned, null, []),
            new ImageEditInstruction([]), null, []);

        await vm.PickFallbackImageCommand.ExecuteAsync(overlay);

        overlay.FallbackImagePath.Should().BeNull();
    }
}

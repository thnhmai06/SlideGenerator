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
using SlideGenerator.Generator;
using SlideGenerator.Recipe.Formats;
using SlideGenerator.Recipe.Services;
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
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: RunDialogViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.Recipes.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Generator;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="RunDialogViewModel" />'s preview gating: an input problem or a real output-path
///     conflict blocks Start, while the non-blocking overwrite warning only surfaces
///     <see cref="RunDialogViewModel.HasOverwriteWarnings" />.
/// </summary>
public sealed class RunDialogViewModelTests
{
    private const string SaveFolder = @"C:\output";

    private static async Task<RunDialogViewModel> CreatePreviewedViewModelAsync(PlannedJob job)
    {
        var service = Substitute.For<IService>();
        service.PreviewAsync(Arg.Any<Request>(), Arg.Any<CancellationToken>())
            .Returns(new List<PlannedJob> { job });
        var viewModel = new RunDialogViewModel(service, Substitute.For<IFilePicker>())
        {
            SaveFolder = SaveFolder
        };
        viewModel.Initialize(1, "Recipe");

        await WaitForPreviewAsync(viewModel);

        return viewModel;
    }

    private static async Task WaitForPreviewAsync(RunDialogViewModel viewModel)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (viewModel.IsPreviewLoading || viewModel.PlannedJobs.Count == 0)
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The run dialog preview did not finish within 10 seconds.");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private static PlannedJob Job(ConflictKind conflictKind, JobProblem problem = JobProblem.None)
    {
        return new PlannedJob(Path.Combine(SaveFolder, "out.pptx"), "wb.xlsx", "Sheet1", conflictKind, problem);
    }

    /// <summary>An overwrite warning does not block Start, and is reported separately as a warning.</summary>
    [Fact]
    public async Task Preview_OverwritesExistingFile_DoesNotBlockButWarns()
    {
        var viewModel = await CreatePreviewedViewModelAsync(Job(ConflictKind.OverwritesExistingFile));

        viewModel.HasBlockedJobs.Should().BeFalse();
        viewModel.HasOverwriteWarnings.Should().BeTrue();
        viewModel.StartCommand.CanExecute(null).Should().BeTrue();
    }

    /// <summary>An input problem blocks Start and is not treated as an overwrite warning.</summary>
    [Fact]
    public async Task Preview_WorkbookNotFound_BlocksStart()
    {
        var viewModel = await CreatePreviewedViewModelAsync(
            Job(ConflictKind.None, JobProblem.WorkbookNotFound));

        viewModel.HasBlockedJobs.Should().BeTrue();
        viewModel.HasOverwriteWarnings.Should().BeFalse();
        viewModel.StartCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>A duplicate output path within the request blocks Start.</summary>
    [Fact]
    public async Task Preview_DuplicateWithinRequest_BlocksStart()
    {
        var viewModel = await CreatePreviewedViewModelAsync(Job(ConflictKind.DuplicateWithinRequest));

        viewModel.HasBlockedJobs.Should().BeTrue();
        viewModel.StartCommand.CanExecute(null).Should().BeFalse();
    }

    /// <summary>A clean job starts and produces no warning.</summary>
    [Fact]
    public async Task Preview_CleanJob_CanStartWithoutWarnings()
    {
        var viewModel = await CreatePreviewedViewModelAsync(Job(ConflictKind.None));

        viewModel.HasBlockedJobs.Should().BeFalse();
        viewModel.HasOverwriteWarnings.Should().BeFalse();
        viewModel.StartCommand.CanExecute(null).Should().BeTrue();
    }
}

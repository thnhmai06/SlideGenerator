/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: IDialogService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Desktop.Features.Recipes.ViewModels;
using SlideGenerator.Desktop.Features.Recipes.Views;

namespace SlideGenerator.Desktop.Services.Dialogs;

/// <summary>
///     Modal dialogs a ViewModel can request without referencing any <see cref="Avalonia.Controls.Window" />
///     directly. Extended with view-specific dialogs (e.g. the run confirmation dialog) as those features
///     are built — kept minimal for now rather than declaring speculative methods ahead of their callers.
/// </summary>
public interface IDialogService
{
    /// <summary>Shows a yes/no confirmation dialog. Returns <see langword="true" /> if the user confirmed.</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel, string cancelLabel);

    /// <summary>
    ///     Shows the run confirmation dialog for a recipe. Returns the new request id if the user started a
    ///     run, or <see langword="null" /> if they canceled.
    /// </summary>
    Task<string?> ShowRunDialogAsync(int recipeId, string recipeName);
}

/// <inheritdoc cref="IDialogService" />
public sealed class DialogService(IServiceProvider serviceProvider) : IDialogService
{
    private static Window? Owner =>
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow;

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string confirmLabel, string cancelLabel)
    {
        var dialog = new ConfirmDialog { Title = title, Message = message, ConfirmLabel = confirmLabel, CancelLabel = cancelLabel };
        return Owner is not null && await dialog.ShowDialog<bool>(Owner).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> ShowRunDialogAsync(int recipeId, string recipeName)
    {
        if (Owner is null) return null;

        var viewModel = serviceProvider.GetRequiredService<RunDialogViewModel>();
        viewModel.Initialize(recipeId, recipeName);
        var dialog = new RunDialogView { DataContext = viewModel };
        var started = await dialog.ShowDialog<bool>(Owner).ConfigureAwait(false);
        return started ? viewModel.CreatedRequestId : null;
    }
}

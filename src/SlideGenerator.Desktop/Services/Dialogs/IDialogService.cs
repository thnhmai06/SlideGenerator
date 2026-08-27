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
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Desktop.Features.RecipeEditor.Views;
using SlideGenerator.Recipe.Models;

namespace SlideGenerator.Desktop.Services.Dialogs;

/// <summary>
///     Modal dialogs a ViewModel can request without referencing any <see cref="Avalonia.Controls.Window" />
///     directly. Extended with view-specific dialogs (e.g. the run confirmation dialog) as those features
///     are built — kept minimal for now rather than declaring speculative methods ahead of their callers.
/// </summary>
public interface IDialogService
{
    /// <summary>Shows a yes/no confirmation dialog. Returns <see langword="true" /> if the user confirmed.
    ///     Pass <paramref name="danger" /> for an irreversible/destructive action (plan §4.2: "icon + variant
    ///     danger — nút đỏ khi destructive") so the confirm button renders as a warning rather than the
    ///     default affirmative color.</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel, string cancelLabel, bool danger = false);

    /// <summary>
    ///     Shows the run confirmation dialog for a recipe. Returns the new request id if the user started a
    ///     run, or <see langword="null" /> if they canceled.
    /// </summary>
    Task<string?> ShowRunDialogAsync(int recipeId, string recipeName);

    /// <summary>
    ///     Shows the template picker dialog (pick a presentation, then one of its slides). Returns the picked
    ///     template, or <see langword="null" /> if the user canceled.
    /// </summary>
    Task<PresentationSource?> ShowTemplatePickerAsync();
}

/// <inheritdoc cref="IDialogService" />
public sealed class DialogService(IServiceProvider serviceProvider) : IDialogService
{
    private static Window? Owner =>
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow;

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string confirmLabel, string cancelLabel, bool danger = false)
    {
        var dialog = new ConfirmDialog
        {
            Title = title, Message = message, ConfirmLabel = confirmLabel, CancelLabel = cancelLabel, IsDanger = danger
        };
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

    /// <inheritdoc />
    public async Task<PresentationSource?> ShowTemplatePickerAsync()
    {
        if (Owner is null) return null;

        var viewModel = serviceProvider.GetRequiredService<TemplatePickerViewModel>();
        var dialog = new TemplatePickerView { DataContext = viewModel };
        var picked = await dialog.ShowDialog<bool>(Owner).ConfigureAwait(false);
        return picked ? viewModel.Result : null;
    }
}

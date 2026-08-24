/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: Registration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Desktop.Features.Recipes.ViewModels;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Features.RecipeEditor.ViewModels;
using SlideGenerator.Desktop.Features.Runs.ViewModels;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Desktop.Services.Theme;
using SlideGenerator.Desktop.Shell;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop;

/// <summary>
///     Registers every Desktop-host-owned service: the in-process progress/log event bus that
///     <c>SlideGenerator.Generator</c>'s <c>Service</c> depends on (ViewModels subscribe to
///     <see cref="GeneratingEventBus" />/<see cref="LogNotifier" /> directly — there is no IPC layer),
///     <see cref="IProgressHub" />, localization, theme, dialogs/file picker, and Shell ViewModels.
/// </summary>
public static class Registration
{
    /// <summary>Adds every Desktop-host service to the DI container.</summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        services.AddSingleton<GeneratingEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<GeneratingEventBus>());

        services.AddSingleton<LogNotifier>();
        services.AddSingleton<ILogNotifier>(sp => sp.GetRequiredService<LogNotifier>());

        services.AddSingleton<IProgressHub, ProgressHub>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFilePicker, FilePicker>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SplashViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<RunsViewModel>();
        services.AddSingleton<RecipesViewModel>();
        // Transient — a fresh instance per Run dialog invocation, unlike page ViewModels which are cached.
        services.AddTransient<RunDialogViewModel>();

        services.AddSingleton<ISummaryCache, SummaryCache>();
        // Transient — a fresh instance per editor session, same reasoning as RunDialogViewModel.
        services.AddTransient<RecipeEditorViewModel>();
        // Transient — a fresh instance per template-picker invocation, same reasoning as RunDialogViewModel.
        services.AddTransient<TemplatePickerViewModel>();

        return services;
    }
}
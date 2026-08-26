/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ShellViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Desktop.Features.Recipes.ViewModels;
using SlideGenerator.Desktop.Features.Runs.ViewModels;
using SlideGenerator.Desktop.Features.Settings.ViewModels;
using SlideGenerator.Desktop.Services.Theme;
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Desktop.Shell;

/// <summary>
///     Owns the four fixed shell destinations (<see cref="ShellDestination" />) and the currently-shown page,
///     plus the title toolbar's theme-toggle (the toolbar itself — <see cref="TitleToolbar" /> — binds
///     directly to this VM, inherited from <see cref="ShellView" />'s own DataContext; it needs no DI wiring
///     of its own). Each destination's page ViewModel is resolved lazily on first navigation via
///     <see cref="IServiceProvider" /> rather than all three being constructed eagerly at startup — a page's
///     dependencies (repositories, etc.) should not pay their construction cost until the user actually opens
///     that page. State is preserved across navigations (the resolved instance is cached in
///     <see cref="_pages" />), matching the plan's "no state lost when switching sidebar items" requirement.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly Dictionary<ShellDestination, ObservableObject> _pages = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly IThemeService _themeService;
    private readonly ISettingManager _settingManager;

    [ObservableProperty] private ShellDestination _currentDestination;
    [ObservableProperty] private ObservableObject? _currentPage;

    /// <summary>Constructs the shell and navigates to <see cref="ShellDestination.Recipes" />.</summary>
    /// <param name="serviceProvider">Resolves each destination's page ViewModel on first visit.</param>
    /// <param name="themeService">Applies the next theme when <see cref="ToggleThemeCommand" /> runs.</param>
    /// <param name="settingManager">Reads the current theme mode to compute the next one in the cycle.</param>
    public ShellViewModel(IServiceProvider serviceProvider, IThemeService themeService, ISettingManager settingManager)
    {
        _serviceProvider = serviceProvider;
        _themeService = themeService;
        _settingManager = settingManager;
        Navigate(ShellDestination.Recipes);
    }

    /// <summary>Cycles <c>Appearance.Theme</c> System → Light → Dark → System, switching instantly (no reveal).</summary>
    [RelayCommand]
    private Task ToggleTheme()
    {
        return ToggleThemeAsync(null);
    }

    /// <summary>
    ///     Same cycle as <see cref="ToggleThemeCommand" />, but reveals the switch as a circle expanding from
    ///     <paramref name="origin" /> — <see cref="TitleToolbar" />'s theme button calls this directly (not
    ///     through the command) since the origin is a view-layer concept (the button's own on-screen position)
    ///     that doesn't belong on this ViewModel as state.
    /// </summary>
    public Task ToggleThemeAsync(Point? origin)
    {
        var next = _settingManager.Current.Appearance.Theme switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            _ => ThemeMode.System
        };
        return origin is { } o
            ? _themeService.SetThemeAnimatedAsync(next, o)
            : _themeService.SetThemeAsync(next);
    }

    /// <summary>Gets whether <see cref="ShellDestination.Recipes" /> is the active destination.</summary>
    public bool IsRecipesActive => CurrentDestination == ShellDestination.Recipes;

    /// <summary>Gets whether <see cref="ShellDestination.Runs" /> is the active destination.</summary>
    public bool IsRunsActive => CurrentDestination == ShellDestination.Runs;

    /// <summary>Gets whether <see cref="ShellDestination.Settings" /> is the active destination.</summary>
    public bool IsSettingsActive => CurrentDestination == ShellDestination.Settings;

    /// <summary>Gets whether <see cref="ShellDestination.About" /> is the active destination.</summary>
    public bool IsAboutActive => CurrentDestination == ShellDestination.About;

    [RelayCommand]
    private void Navigate(ShellDestination destination)
    {
        CurrentDestination = destination;
        if (!_pages.TryGetValue(destination, out var page))
        {
            page = ResolvePage(destination);
            _pages[destination] = page;
        }

        CurrentPage = page;
    }

    partial void OnCurrentDestinationChanged(ShellDestination value)
    {
        OnPropertyChanged(nameof(IsRecipesActive));
        OnPropertyChanged(nameof(IsRunsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
        OnPropertyChanged(nameof(IsAboutActive));
    }

    // Swapped for the real page ViewModel as each feature lands (Runs/Recipes/Settings done; About is the
    // PlaceholderPageViewModel default branch below until the overhaul's P6 builds its real page) — resolved
    // via _serviceProvider since each page's dependencies vary and shouldn't be threaded through
    // ShellViewModel's own constructor.
    private ObservableObject ResolvePage(ShellDestination destination)
    {
        switch (destination)
        {
            case ShellDestination.Runs:
                return _serviceProvider.GetRequiredService<RunsViewModel>();
            case ShellDestination.Recipes:
                var recipes = _serviceProvider.GetRequiredService<RecipesViewModel>();
                recipes.RunStarted += OnRunStartedFromRecipes;
                return recipes;
            case ShellDestination.Settings:
                return _serviceProvider.GetRequiredService<SettingsViewModel>();
            default:
                return new PlaceholderPageViewModel(destination.ToString());
        }
    }

    // After a run starts from Recipes, jump to Runs with the new request preselected (plan §5.1 step 23).
    private void OnRunStartedFromRecipes(string requestId)
    {
        Navigate(ShellDestination.Runs);
        if (_pages[ShellDestination.Runs] is RunsViewModel runs) _ = runs.SelectRequestAsync(requestId);
    }
}

/// <summary>
///     Stand-in page shown for a destination whose real page has not been built yet in the current phase
///     sequence. Not a permanent feature — removed once every destination has a real page ViewModel.
/// </summary>
public sealed partial class PlaceholderPageViewModel(string title) : ObservableObject
{
    /// <summary>Gets the destination name shown while its real page is not yet built.</summary>
    public string Title { get; } = title;
}

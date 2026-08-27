/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: ViewConstructionTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless;
using NSubstitute;
using SlideGenerator.Desktop.Features.About.Models;
using SlideGenerator.Desktop.Features.About.Services;
using SlideGenerator.Desktop.Features.About.ViewModels;
using SlideGenerator.Desktop.Features.About.Views;
using SlideGenerator.Desktop.Features.Recipes.ViewModels;
using SlideGenerator.Desktop.Features.Recipes.Views;
using SlideGenerator.Desktop.Features.RecipeEditor.Services;
using SlideGenerator.Desktop.Features.Runs.ViewModels;
using SlideGenerator.Desktop.Features.Runs.Views;
using SlideGenerator.Desktop.Features.Settings.ViewModels;
using SlideGenerator.Desktop.Features.Settings.Views;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Desktop.Services.Theme;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Recipe.Services;
using SlideGenerator.Settings.Mutable;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Smoke tests (plan §7.1: "mỗi View construct + resource resolve") for the four pages reachable from
///     <c>ShellView</c>'s nav pill/toolbar — constructs each with a real ViewModel (mocked dependencies, same
///     pattern as this project's other ViewModel tests) as <c>DataContext</c>, shows it in a headless
///     <see cref="Window" />, and asserts nothing throws. A <c>{StaticResource}</c> key typo'd or removed from
///     <c>Resources/*.axaml</c> that a view still references throws at this point — the same class of
///     regression a live smoke run would catch, but automated and fast. Does not assert on rendered pixels or
///     specific bound values — that's each ViewModel's own unit tests' job (see
///     <see cref="RecipesViewModelTests" />/<see cref="SettingsViewModelTests" />/etc.).
///     <para>
///         Uses <see cref="HeadlessUnitTestSession" /> directly instead of the <c>[AvaloniaFact]</c>
///         attribute/<c>Avalonia.Headless.XUnit</c> package: that package's <c>AvaloniaFactDiscoverer</c>
///         reflects into an internal <c>Xunit.v3.TestIntrospectionHelper.GetTestCaseDetails</c> overload that
///         changed shape in <c>xunit.v3</c> 4.0.0 (this repo's pinned version, matching all 10 test
///         projects) — <c>Avalonia.Headless.XUnit</c> 12.1.1 (confirmed latest on NuGet) throws
///         <c>MissingMethodException</c> at test-discovery time against it. <see cref="HeadlessUnitTestSession" />
///         itself has no xunit dependency at all — it is Avalonia's own dispatcher-marshalling primitive, so
///         driving it from a plain <c>[Fact]</c> sidesteps the incompatible discoverer entirely while keeping
///         the real <see cref="App" /> resource pipeline (same reasoning the deleted <c>TestAppBuilder.cs</c>
///         doc comment gave: the headless platform never reaches
///         <see cref="App.OnFrameworkInitializationCompleted" />'s desktop-lifetime branch, so only
///         <see cref="App.Initialize" /> — loading <c>App.axaml</c>'s resources/styles — actually runs).
///     </para>
/// </summary>
public sealed class ViewConstructionTests
{
    private static readonly HeadlessUnitTestSession Session = HeadlessTestSession.Instance;

    [Fact]
    public Task RecipesView_ConstructsAndShows_WithoutThrowing()
    {
        return Session.Dispatch(() =>
        {
            var repository = Substitute.For<IRecipeRepository>();
            repository.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<IRecipeMetadata>());
            var vm = new RecipesViewModel(repository, Substitute.For<IRecipePackageService>(), Substitute.For<IService>(),
                Substitute.For<IDialogService>(), Substitute.For<IFilePicker>(), Substitute.For<IServiceProvider>(),
                Substitute.For<ISummaryCache>());

            var window = new Window { Content = new RecipesView { DataContext = vm } };
            window.Show();
        }, CancellationToken.None);
    }

    [Fact]
    public Task RunsView_ConstructsAndShows_WithoutThrowing()
    {
        return Session.Dispatch(() =>
        {
            var progressHub = Substitute.For<IProgressHub>();
            progressHub.Jobs.Returns(new ObservableCollection<JobSnapshot>());
            progressHub.Rows.Returns(new ObservableCollection<RowProgress>());
            progressHub.Logs.Returns(new ObservableCollection<LogEntry>());
            var service = Substitute.For<IService>();
            service.ListActiveAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<string, Summary>());
            service.ListCompletedAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<string, Summary>());
            var vm = new RunsViewModel(service, progressHub, Substitute.For<IDialogService>());

            var window = new Window { Content = new RunsView { DataContext = vm } };
            window.Show();
        }, CancellationToken.None);
    }

    [Fact]
    public Task SettingsView_ConstructsAndShows_WithoutThrowing()
    {
        return Session.Dispatch(() =>
        {
            var manager = Substitute.For<ISettingManager>();
            manager.Current.Returns(new Setting());
            var vm = new SettingsViewModel(manager, Substitute.For<IThemeService>(), Substitute.For<ILocalizationService>());

            var window = new Window { Content = new SettingsView { DataContext = vm } };
            window.Show();
        }, CancellationToken.None);
    }

    [Fact]
    public Task AboutView_ConstructsAndShows_WithoutThrowing()
    {
        return Session.Dispatch(() =>
        {
            var dataService = Substitute.For<IAboutDataService>();
            dataService.GetContributorsAsync(Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Contributor>)new List<Contributor>());
            dataService.GetSupportersAsync(Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Supporter>)new List<Supporter>());
            var vm = new AboutViewModel(dataService);

            var window = new Window { Content = new AboutView { DataContext = vm } };
            window.Show();
        }, CancellationToken.None);
    }
}

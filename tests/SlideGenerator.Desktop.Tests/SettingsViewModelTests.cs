/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: SettingsViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.Settings.ViewModels;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Theme;
using SlideGenerator.Settings.Mutable;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="SettingsViewModel" /> — loading the current <see cref="Setting" /> into its
///     bound properties without re-triggering a save, and persisting real user edits through
///     <see cref="ISettingManager" />/<see cref="IThemeService" />/<see cref="ILocalizationService" />.
/// </summary>
public sealed class SettingsViewModelTests
{
    private static ISettingManager CreateManager(Setting? initial = null)
    {
        var manager = Substitute.For<ISettingManager>();
        manager.Current.Returns(initial ?? new Setting());
        return manager;
    }

    [Fact]
    public void Constructor_LoadsCurrentSettingsWithoutPersisting()
    {
        var setting = new Setting
        {
            Appearance = new Setting.AppearanceSetting { Theme = ThemeMode.Dark, Language = "en", ReducedMotion = true },
            Performance = new Setting.PerformanceSetting { MaxConcurrentJobs = 8 }
        };
        var manager = CreateManager(setting);
        var themeService = Substitute.For<IThemeService>();
        var localization = Substitute.For<ILocalizationService>();

        var vm = new SettingsViewModel(manager, themeService, localization);

        vm.Theme.Should().Be(ThemeMode.Dark);
        vm.Language.Should().Be("en");
        vm.ReducedMotion.Should().BeTrue();
        vm.MaxConcurrentJobs.Should().Be(8u);
        themeService.DidNotReceiveWithAnyArgs().SetThemeAsync(default);
        localization.DidNotReceiveWithAnyArgs().SetLanguage(default!);
        manager.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task ThemeChanged_AppliesViaThemeService()
    {
        var manager = CreateManager();
        var themeService = Substitute.For<IThemeService>();
        var vm = new SettingsViewModel(manager, themeService, Substitute.For<ILocalizationService>());

        vm.Theme = ThemeMode.Dark;
        await Task.Yield();

        await themeService.Received(1).SetThemeAsync(ThemeMode.Dark);
    }

    [Fact]
    public async Task LanguageChanged_AppliesViaLocalizationServiceAndPersists()
    {
        var manager = CreateManager();
        var localization = Substitute.For<ILocalizationService>();
        var vm = new SettingsViewModel(manager, Substitute.For<IThemeService>(), localization);

        vm.Language = "en";
        await Task.Yield();

        localization.Received(1).SetLanguage("en");
        await manager.Received(1).Update(Arg.Is<Setting>(s => s.Appearance.Language == "en"));
    }

    [Fact]
    public async Task MaxConcurrentJobsChanged_Persists()
    {
        var manager = CreateManager();
        var vm = new SettingsViewModel(manager, Substitute.For<IThemeService>(), Substitute.For<ILocalizationService>());

        vm.MaxConcurrentJobs = 12;
        await Task.Yield();

        await manager.Received(1).Update(Arg.Is<Setting>(s => s.Performance.MaxConcurrentJobs == 12));
    }

    [Fact]
    public async Task ProxyFieldsChanged_PersistsUnderNetworkProxy()
    {
        var manager = CreateManager();
        var vm = new SettingsViewModel(manager, Substitute.For<IThemeService>(), Substitute.For<ILocalizationService>())
        {
            UseProxy = true,
            ProxyAddress = "http://proxy:8080",
            ProxyUsername = "user"
        };
        await Task.Yield();

        await manager.Received().Update(Arg.Is<Setting>(s =>
            s.Network.Proxy.UseProxy && s.Network.Proxy.ProxyAddress == "http://proxy:8080" && s.Network.Proxy.Username == "user"));
    }

    [Fact]
    public async Task ResetPerformanceCommand_ResetsOnlyPerformanceGroup()
    {
        var setting = new Setting
        {
            Appearance = new Setting.AppearanceSetting { Theme = ThemeMode.Dark },
            Performance = new Setting.PerformanceSetting { MaxConcurrentJobs = 15 }
        };
        var manager = CreateManager(setting);
        Setting? persisted = null;
        manager.Update(Arg.Do<Setting>(s => persisted = s)).Returns(Task.CompletedTask);
        // After Update mutates `persisted`, the VM's next Current read must reflect it — LoadFromSettings()
        // runs right after ResetPerformanceCommand persists, so Current has to be the *new* value here.
        manager.Current.Returns(_ => persisted ?? setting);

        var vm = new SettingsViewModel(manager, Substitute.For<IThemeService>(), Substitute.For<ILocalizationService>());

        await vm.ResetPerformanceCommand.ExecuteAsync(null);

        persisted!.Performance.MaxConcurrentJobs.Should().Be(5u); // PerformanceSetting's default
        persisted.Appearance.Theme.Should().Be(ThemeMode.Dark); // untouched
        vm.MaxConcurrentJobs.Should().Be(5u);
    }
}

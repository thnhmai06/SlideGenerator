/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: ThemeServiceTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Avalonia;
using Avalonia.Headless;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Services.Theme;
using SlideGenerator.Settings.Mutable;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit test for <see cref="ThemeService.ApplyFromSettings" />'s reduced-motion branch (plan §7.1: "theme
///     service reduced-motion branch" test; also the app-crash bug this session found and fixed —
///     <c>ApplyReducedMotion</c> must read <c>Tokens.axaml</c>'s durations via
///     <see cref="Application.TryGetResource(object,Avalonia.Styling.ThemeVariant?,out object?)" />, which
///     walks merged dictionaries, not the dictionary indexer, which doesn't). Needs a real headless Avalonia
///     app (see <see cref="ViewConstructionTests" />'s doc comment for why <see cref="HeadlessUnitTestSession" />
///     is used directly instead of <c>[AvaloniaFact]</c>) since <c>Application.Current</c>/<c>Resources</c> must
///     actually exist.
/// </summary>
public sealed class ThemeServiceTests
{
    private static readonly HeadlessUnitTestSession Session = HeadlessTestSession.Instance;

    [Fact]
    public Task ApplyFromSettings_ReducedMotionToggled_ZeroesAndRestoresMotionTokens()
    {
        return Session.Dispatch(() =>
        {
            var settingManager = Substitute.For<ISettingManager>();
            var reduced = new Setting { Appearance = new Setting.AppearanceSetting { ReducedMotion = true } };
            var normal = new Setting { Appearance = new Setting.AppearanceSetting { ReducedMotion = false } };
            settingManager.Current.Returns(reduced);
            var themeService = new ThemeService(settingManager);

            themeService.ApplyFromSettings();

            var app = Application.Current!;
            app.Resources["MotionMicro"].Should().Be(TimeSpan.Zero);
            app.Resources["MotionUi"].Should().Be(TimeSpan.Zero);
            app.Resources["MotionBrand"].Should().Be(TimeSpan.Zero);

            settingManager.Current.Returns(normal);
            themeService.ApplyFromSettings();

            app.Resources["MotionMicro"].Should().NotBe(TimeSpan.Zero);
            app.Resources["MotionUi"].Should().NotBe(TimeSpan.Zero);
            app.Resources["MotionBrand"].Should().NotBe(TimeSpan.Zero);
        }, CancellationToken.None);
    }
}

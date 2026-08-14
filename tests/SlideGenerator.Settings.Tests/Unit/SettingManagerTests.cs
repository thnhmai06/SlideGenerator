/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings.Tests
 * File: SettingManagerTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Settings.Mutable;
using Xunit;

namespace SlideGenerator.Settings.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SettingManager" />, verifying load/save lifecycle (including real
///     JSON round-tripping to disk), defaults reset, and state propagation. Each test runs in
///     isolation against a unique temp file path (via the test-only <c>filePath</c> constructor
///     override), never touching the real user settings file.
/// </summary>
public sealed class SettingManagerTests : IDisposable
{
    private static readonly Setting DefaultSetting = new();
    private readonly ILogger<SettingManager> _logger = NullLogger<SettingManager>.Instance;
    private readonly SettingManager _manager;
    private readonly string _testFilePath;

    /// <summary>
    ///     Initializes a new <see cref="SettingManager" /> pointed at a unique temp file so tests
    ///     never collide with each other or with the real settings file.
    /// </summary>
    public SettingManagerTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"SlideGeneratorSettingsTest_{Guid.NewGuid():N}.json");
        _manager = new SettingManager(_logger, _testFilePath);
    }

    /// <summary>Deletes the temp settings file created by the test, if any.</summary>
    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    #region ResetToDefaults

    /// <summary>
    ///     Verifies that <see cref="SettingManager.ResetToDefaults" /> replaces <see cref="SettingManager.Current" />
    ///     with a default <see cref="Setting" /> instance regardless of any previously applied configuration.
    /// </summary>
    [Fact]
    public async Task ResetToDefaults_WithCustomSetting_SetsCurrentToDefault()
    {
        var custom = new Setting
        {
            Performance = new Setting.PerformanceSetting { MaxConcurrentJobs = 42u }
        };
        await _manager.Update(custom);

        await _manager.ResetToDefaults();

        _manager.Current.Should().Be(DefaultSetting);
    }

    #endregion

    #region Update

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Update" /> replaces <see cref="SettingManager.Current" />
    ///     with the supplied <see cref="Setting" /> instance and persists it to disk as JSON.
    /// </summary>
    [Fact]
    public async Task Update_NewSetting_UpdatesCurrentAndPersistsToDisk()
    {
        var newSetting = new Setting
        {
            Performance = new Setting.PerformanceSetting { MaxConcurrentJobs = 7u }
        };

        await _manager.Update(newSetting);

        _manager.Current.Should().Be(newSetting);
        File.Exists(_testFilePath).Should().BeTrue();
        (await File.ReadAllTextAsync(_testFilePath, TestContext.Current.CancellationToken)).Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Save

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Save" /> writes <see cref="SettingManager.Current" />'s
    ///     field values to disk without any transformation.
    /// </summary>
    [Fact]
    public async Task Save_CurrentSetting_WritesFieldsToDisk()
    {
        var setting = new Setting
        {
            Network = new Setting.NetworkSetting
            {
                Proxy = new Setting.Proxy { Password = "plain-text" }
            }
        };

        await _manager.Update(setting);

        var content = await File.ReadAllTextAsync(_testFilePath, TestContext.Current.CancellationToken);
        content.Should().Contain("plain-text");
    }

    #endregion

    #region Load

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Load" /> returns <see langword="false" /> when the settings
    ///     file does not exist on disk, leaves <see cref="SettingManager.Current" /> at its default value, and
    ///     creates the file on disk with those defaults.
    /// </summary>
    [Fact]
    public async Task Load_SettingsFileNotFound_ReturnsFalseAndCreatesDefaultFile()
    {
        // File does not exist at _testFilePath

        var result = await _manager.Load();

        result.Should().BeFalse();
        _manager.Current.Should().Be(DefaultSetting);
        File.Exists(_testFilePath).Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Load" /> returns <see langword="true" /> when the settings file
    ///     exists, deserializes its JSON content, and updates <see cref="SettingManager.Current" />.
    /// </summary>
    [Fact]
    public async Task Load_SettingsFilePresent_ReturnsTrueAndUpdatesCurrent()
    {
        var writer = new SettingManager(_logger, _testFilePath);
        await writer.Update(new Setting
        {
            Performance = new Setting.PerformanceSetting { MaxConcurrentJobs = 99u }
        });

        var result = await _manager.Load();

        result.Should().BeTrue();
        _manager.Current.Performance.MaxConcurrentJobs.Should().Be(99u);
    }

    #endregion
}
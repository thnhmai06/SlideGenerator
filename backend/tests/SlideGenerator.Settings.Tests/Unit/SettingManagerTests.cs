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
using NSubstitute;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Settings.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Settings.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SettingManager" />, verifying load/save lifecycle,
///     defaults reset, and state propagation. Each test runs in isolation using a unique temporary file path.
/// </summary>
public sealed class SettingManagerTests : IDisposable
{
    private static readonly Setting DefaultSetting = new();
    private readonly ILogger<SettingManager> _logger = NullLogger<SettingManager>.Instance;
    private readonly SettingManager _manager;
    private readonly ISerializer _serializer = Substitute.For<ISerializer>();
    private readonly string _testFilePath;

    public SettingManagerTests()
    {
        var testExt = $".test{Guid.NewGuid():N}";
        _serializer.FileExtension.Returns(testExt);
        _testFilePath = NameAndPaths.SettingsFile.GetFilePath(testExt);
        Directory.CreateDirectory(Path.GetDirectoryName(_testFilePath)!);
        _manager = new SettingManager(_serializer, _logger);
    }

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
        _serializer.Serialize(Arg.Any<Setting>()).Returns("serialized-content");
        var custom = new Setting
        {
            Performance = new Setting.PerformanceSetting { MaxParallelDownloadImage = 42u }
        };
        await _manager.Update(custom);

        await _manager.ResetToDefaults();

        _manager.Current.Should().Be(DefaultSetting);
    }

    #endregion

    #region Update

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Update" /> replaces <see cref="SettingManager.Current" />
    ///     with the supplied <see cref="Setting" /> instance and persists it by calling the serializer.
    /// </summary>
    [Fact]
    public async Task Update_NewSetting_UpdatesCurrentAndPersists()
    {
        _serializer.Serialize(Arg.Any<Setting>()).Returns("serialized-content");
        var newSetting = new Setting
        {
            Performance = new Setting.PerformanceSetting { MaxParallelEditImage = 7u }
        };

        await _manager.Update(newSetting);

        _manager.Current.Should().Be(newSetting);
        _serializer.Received(1).Serialize(Arg.Any<Setting>());
    }

    #endregion

    #region Save

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Save" /> passes <see cref="SettingManager.Current" /> directly
    ///     to the serializer without any transformation.
    /// </summary>
    [Fact]
    public async Task Save_CurrentSetting_SerializesDirectly()
    {
        var setting = new Setting
        {
            Network = new Setting.NetworkSetting
            {
                Proxy = new Setting.Proxy { Password = "plain-text" }
            }
        };
        _serializer.Serialize(Arg.Any<Setting>()).Returns("serialized-content");

        await _manager.Update(setting);

        _serializer.Received(1).Serialize(Arg.Is<Setting>(s =>
            s.Network.Proxy.Password == "plain-text"));
    }

    #endregion

    #region Load

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Load" /> returns <see langword="false" /> when the settings
    ///     file does not exist on disk, leaving <see cref="SettingManager.Current" /> at its default value.
    /// </summary>
    [Fact]
    public async Task Load_SettingsFileNotFound_ReturnsFalse()
    {
        // File does not exist at _testFilePath

        var result = await _manager.Load();

        result.Should().BeFalse();
        _manager.Current.Should().Be(DefaultSetting);
    }

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Load" /> returns <see langword="true" /> when the settings file
    ///     exists, deserializes its content, and updates <see cref="SettingManager.Current" />.
    /// </summary>
    [Fact]
    public async Task Load_SettingsFilePresent_ReturnsTrueAndUpdatesCurrent()
    {
        var expected = new Setting
        {
            Performance = new Setting.PerformanceSetting { MaxParallelDownloadImage = 99u }
        };
        await File.WriteAllTextAsync(_testFilePath, "serialized-content", TestContext.Current.CancellationToken);
        _serializer.Deserialize<Setting>("serialized-content").Returns(expected);

        var result = await _manager.Load();

        result.Should().BeTrue();
        _manager.Current.Performance.MaxParallelDownloadImage.Should().Be(99u);
    }

    #endregion
}
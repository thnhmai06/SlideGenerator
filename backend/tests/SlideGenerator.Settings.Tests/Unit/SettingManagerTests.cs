/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings.Tests
 * File: SettingManagerTests.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlideGenerator.Cryptography.Application.Abstractions;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Settings.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Settings.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SettingManager" />, verifying load/save lifecycle, encryption integration,
///     defaults reset, and state propagation. Each test runs in isolation using a unique temporary file path.
/// </summary>
public sealed class SettingManagerTests : IDisposable
{
    private static readonly Setting DefaultSetting = new();
    private readonly IEncrypter _encrypter = Substitute.For<IEncrypter>();
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
        _manager = new SettingManager(_encrypter, _serializer, _logger);
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

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Load" /> calls <see cref="IEncrypter.Decrypt" /> on the
    ///     proxy password when a non-empty password is present in the deserialized settings, and that the
    ///     decrypted plain-text value is stored in <see cref="SettingManager.Current" />.
    /// </summary>
    [Fact]
    public async Task Load_ProxyPasswordPresent_CallsDecryptAndStoresPlainText()
    {
        var encryptedSetting = new Setting
        {
            Network = new Setting.NetworkSetting
            {
                Proxy = new Setting.Proxy { Password = "cipher-text" }
            }
        };
        await File.WriteAllTextAsync(_testFilePath, "serialized-content", TestContext.Current.CancellationToken);
        _serializer.Deserialize<Setting>("serialized-content").Returns(encryptedSetting);
        _encrypter.Decrypt("cipher-text").Returns("plain-text");

        await _manager.Load();

        _encrypter.Received(1).Decrypt("cipher-text");
        _manager.Current.Network.Proxy.Password.Should().Be("plain-text");
    }

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Load" /> does NOT call <see cref="IEncrypter.Decrypt" />
    ///     when the proxy password field is empty in the deserialized settings.
    /// </summary>
    [Fact]
    public async Task Load_ProxyPasswordEmpty_DoesNotCallDecrypt()
    {
        var noPasswordSetting = DefaultSetting;
        await File.WriteAllTextAsync(_testFilePath, "serialized-content", TestContext.Current.CancellationToken);
        _serializer.Deserialize<Setting>("serialized-content").Returns(noPasswordSetting);

        await _manager.Load();

        _encrypter.DidNotReceive().Decrypt(Arg.Any<string>());
    }

    #endregion

    #region Save

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Save" /> calls <see cref="IEncrypter.Encrypt" /> on the
    ///     plain-text proxy password before passing the setting object to the serializer.
    /// </summary>
    [Fact]
    public async Task Save_ProxyPasswordPresent_CallsEncryptBeforeSerializing()
    {
        var settingWithPassword = new Setting
        {
            Network = new Setting.NetworkSetting
            {
                Proxy = new Setting.Proxy { Password = "plain-text" }
            }
        };
        _encrypter.Encrypt("plain-text").Returns("cipher-text");
        _serializer.Serialize(Arg.Any<Setting>()).Returns("serialized-content");

        await _manager.Update(settingWithPassword);

        _encrypter.Received(1).Encrypt("plain-text");
        _serializer.Received(1).Serialize(Arg.Is<Setting>(s =>
            s.Network.Proxy.Password == "cipher-text"));
    }

    /// <summary>
    ///     Verifies that <see cref="SettingManager.Save" /> does NOT call <see cref="IEncrypter.Encrypt" />
    ///     when the proxy password is empty, preventing unnecessary encryption overhead.
    /// </summary>
    [Fact]
    public async Task Save_NoProxyPassword_DoesNotCallEncrypt()
    {
        _serializer.Serialize(Arg.Any<Setting>()).Returns("serialized-content");

        await _manager.Update(DefaultSetting);

        _encrypter.DidNotReceive().Encrypt(Arg.Any<string>());
    }

    #endregion
}
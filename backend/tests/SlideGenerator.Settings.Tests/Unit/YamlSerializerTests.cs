/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings.Tests
 * File: YamlSerializerTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Settings.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="YamlSerializer" />, verifying YAML serialization and deserialization
///     of application settings including round-trip fidelity and resilience to unknown properties.
/// </summary>
public sealed class YamlSerializerTests
{
    private readonly YamlSerializer _serializer = new();

    #region FileExtension

    /// <summary>
    ///     Verifies that <see cref="YamlSerializer.FileExtension" /> returns the standard YAML file extension.
    /// </summary>
    [Fact]
    public void FileExtension_Always_ReturnsYamlExtension()
    {
        _serializer.FileExtension.Should().Be(".yaml");
    }

    #endregion

    #region Serialize / Deserialize Round-Trip

    /// <summary>
    ///     Verifies that a default <see cref="Setting" /> survives a full serialize–deserialize round-trip
    ///     with all property values preserved.
    /// </summary>
    [Fact]
    public void Serialize_ThenDeserialize_DefaultSetting_PreservesAllValues()
    {
        var original = new Setting();

        var yaml = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<Setting>(yaml);

        restored.Should().Be(original);
    }

    /// <summary>
    ///     Verifies that a <see cref="Setting" /> with custom performance limits survives a round-trip
    ///     and all non-default property values are preserved.
    /// </summary>
    [Fact]
    public void Serialize_ThenDeserialize_CustomPerformanceSetting_PreservesValues()
    {
        var random = new Random();
        var download = (uint)random.Next(1, 100);
        var editImg = (uint)random.Next(1, 100);
        var editPres = (uint)random.Next(1, 100);
        var readWork = (uint)random.Next(1, 100);
        var readPres = (uint)random.Next(1, 100);

        var original = new Setting
        {
            Performance = new Setting.PerformanceSetting
            {
                MaxParallelDownloadImage = download,
                MaxParallelEditImage = editImg,
                MaxParallelEditPresentation = editPres,
                MaxParallelReadWorkbook = readWork,
                MaxParallelReadPresentation = readPres
            }
        };

        var yaml = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<Setting>(yaml);

        restored.Performance.MaxParallelDownloadImage.Should().Be(download);
        restored.Performance.MaxParallelEditImage.Should().Be(editImg);
        restored.Performance.MaxParallelEditPresentation.Should().Be(editPres);
        restored.Performance.MaxParallelReadWorkbook.Should().Be(readWork);
        restored.Performance.MaxParallelReadPresentation.Should().Be(readPres);
    }

    /// <summary>
    ///     Verifies that a <see cref="Setting" /> with proxy credentials survives a round-trip
    ///     and all network-related fields are restored correctly.
    /// </summary>
    [Fact]
    public void Serialize_ThenDeserialize_ProxySetting_PreservesAllFields()
    {
        var address = $"http://{Guid.NewGuid():N}.com:8080";
        var user = Guid.NewGuid().ToString("N");
        var pass = Guid.NewGuid().ToString("N");
        var domain = Guid.NewGuid().ToString("N");

        var original = new Setting
        {
            Network = new Setting.NetworkSetting
            {
                Proxy = new Setting.Proxy
                {
                    UseProxy = true,
                    ProxyAddress = address,
                    Username = user,
                    Password = pass,
                    Domain = domain
                }
            }
        };

        var yaml = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<Setting>(yaml);

        restored.Network.Proxy.UseProxy.Should().BeTrue();
        restored.Network.Proxy.ProxyAddress.Should().Be(address);
        restored.Network.Proxy.Username.Should().Be(user);
        restored.Network.Proxy.Password.Should().Be(pass);
        restored.Network.Proxy.Domain.Should().Be(domain);
    }

    /// <summary>
    ///     Verifies that <see cref="YamlSerializer.Deserialize{T}" /> does not throw when the YAML source
    ///     contains properties that do not exist on the target type.
    /// </summary>
    [Fact]
    public void Deserialize_UnknownProperties_DoesNotThrow()
    {
        const string yamlWithUnknownFields = """
                                             performance:
                                               maxParallelDownloadImage: 5
                                               unknownFutureField: someValue
                                             unknownSection:
                                               someKey: someValue
                                             """;

        var act = () => _serializer.Deserialize<Setting>(yamlWithUnknownFields);

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Verifies that <see cref="YamlSerializer.Serialize{T}" /> produces non-empty output
    ///     for a default <see cref="Setting" /> instance.
    /// </summary>
    [Fact]
    public void Serialize_DefaultSetting_ProducesNonEmptyYaml()
    {
        var yaml = _serializer.Serialize(new Setting());

        yaml.Should().NotBeNullOrWhiteSpace();
    }

    #endregion
}
/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: YamlSerializerTests.cs
 */

using FluentAssertions;
using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Models;
using Xunit;

namespace SlideGenerator.Tests.Settings;

public sealed class YamlSerializerTests
{
    private readonly YamlSerializer _serializer = new();

    [Fact]
    public void Serialize_ShouldProduceYamlString()
    {
        var setting = new Setting();
        var yaml = _serializer.Serialize(setting);

        yaml.Should().Contain("job:");
        yaml.Should().Contain("maxParallelReadWorkbook:");
    }

    [Fact]
    public void Deserialize_ShouldRestoreObject()
    {
        var yaml = "job:\n  maxParallelReadWorkbook: 10";
        var result = _serializer.Deserialize<Setting>(yaml);

        result.Job.MaxParallelReadWorkbook.Should().Be(10);
    }

    [Fact]
    public void FileExtension_ShouldBeYaml()
    {
        _serializer.FileExtension.Should().Be(".yaml");
    }
}

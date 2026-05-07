/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: YamlSerializerTests.cs
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
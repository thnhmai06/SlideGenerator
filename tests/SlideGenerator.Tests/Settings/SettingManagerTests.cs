/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: SettingManagerTests.cs
 */

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SlideGenerator.Settings.Entities;
using SlideGenerator.Settings.Models;
using SlideGenerator.Settings.Services;
using Xunit;

namespace SlideGenerator.Tests.Settings;

public sealed class SettingManagerTests
{
    [Fact]
    public void DefaultSettings_ShouldHaveExpectedValues()
    {
        var settings = new Setting();
        settings.Job.MaxParallelReadWorkbook.Should().Be(5);
        settings.Download.Temp.FolderPath.Should().NotBeNullOrEmpty();
    }
}

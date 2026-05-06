/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
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
using SlideGenerator.Settings.Models;
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
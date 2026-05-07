/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Tests
 * File: SettingsUtilitiesTests.cs
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
using SlideGenerator.Settings;
using Xunit;

namespace SlideGenerator.Tests.Settings;

public sealed class SettingsUtilitiesTests
{
    [Theory]
    [InlineData("google.com", "https://google.com/")]
    [InlineData("http://test.com", "http://test.com/")]
    [InlineData("  https://secure.link  ", "https://secure.link/")]
    public void NormalizeUri_ShouldPrependHttps_WhenSchemeIsMissing(string input, string expected)
    {
        var result = Utilities.NormalizeUri(input);
        result.Should().NotBeNull();
        result!.AbsoluteUri.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeUri_ShouldReturnNull_WhenInputIsEmpty(string? input)
    {
        Utilities.NormalizeUri(input).Should().BeNull();
    }

    [Theory]
    [InlineData("valid-file.txt", "valid-file.txt")]
    [InlineData("file/with/slashes.txt", "file_with_slashes.txt")]
    [InlineData("file?with*chars.txt", "file_with_chars.txt")]
    [InlineData("   spaces   ", "spaces")]
    public void NormalizeFileName_ShouldReplaceInvalidChars_WithUnderscore(string input, string expected)
    {
        Utilities.NormalizeFileName(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizeFileName_ShouldReturnDefaultValue_WhenInputIsEmpty()
    {
        Utilities.NormalizeFileName(null, "default.txt").Should().Be("default.txt");
        Utilities.NormalizeFileName("   ", "default.txt").Should().Be("default.txt");
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities.Tests
 * File: NormalizationTests.cs
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
using SlideGenerator.Utilities.Helper;
using Xunit;

namespace SlideGenerator.Utilities.Tests.Unit;

/// <summary>
///     Unit tests for the <see cref="Normalization" /> utility class, covering file name sanitization logic.
/// </summary>
public sealed class NormalizationTests
{
    #region SanitizeFileName

    /// <summary>
    ///     Verifies that <see cref="Normalization.SanitizeFileName" /> returns the specified
    ///     <paramref name="defaultValue" /> when the input is <see langword="null" />, empty, or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null, "fallback", "fallback")]
    [InlineData("", "fallback", "fallback")]
    [InlineData("   ", "fallback", "fallback")]
    public void NormalizeFileName_NullOrWhitespace_ReturnsDefaultValue(
        string? input, string defaultValue, string expected)
    {
        var result = Normalization.SanitizeFileName(input, defaultValue);

        result.Should().Be(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="Normalization.SanitizeFileName" /> returns an empty string
    ///     when the input is <see langword="null" /> or empty and no explicit default value is supplied.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NormalizeFileName_NullOrEmptyNoDefault_ReturnsEmpty(string? input)
    {
        var result = Normalization.SanitizeFileName(input);

        result.Should().BeEmpty();
    }

    /// <summary>
    ///     Verifies that <see cref="Normalization.SanitizeFileName" /> replaces characters that are
    ///     invalid in file names (as determined by <see cref="Path.GetInvalidFileNameChars" />) with underscores.
    /// </summary>
    [Theory]
    [InlineData("file<name>", "file_name")]
    [InlineData("C:/path/to/file", "C_path_to_file")]
    [InlineData("name:copy", "name_copy")]
    [InlineData("report|2026", "report_2026")]
    public void NormalizeFileName_ContainsInvalidChars_ReplacesWithUnderscore(string input, string expected)
    {
        var result = Normalization.SanitizeFileName(input);

        result.Should().Be(expected);
    }

    /// <summary>
    ///     Verifies that <see cref="Normalization.SanitizeFileName" /> returns the input unchanged
    ///     when it contains only valid file name characters.
    /// </summary>
    [Fact]
    public void NormalizeFileName_OnlyValidChars_ReturnedUnchanged()
    {
        const string input = "valid-file_name 123.txt";

        var result = Normalization.SanitizeFileName(input);

        result.Should().Be(input);
    }

    /// <summary>
    ///     Verifies that <see cref="Normalization.SanitizeFileName" /> returns the specified default value
    ///     when the entire input consists of invalid characters, leaving an empty normalized string.
    /// </summary>
    [Fact]
    public void NormalizeFileName_AllInvalidChars_ReturnsDefaultValue()
    {
        var allInvalid = new string(Path.GetInvalidFileNameChars());
        const string defaultValue = "fallback";

        var result = Normalization.SanitizeFileName(allInvalid, defaultValue);

        result.Should().Be(defaultValue);
    }

    /// <summary>
    ///     Verifies that <see cref="Normalization.SanitizeFileName" /> trims surrounding whitespace
    ///     from the input before processing.
    /// </summary>
    [Fact]
    public void NormalizeFileName_SurroundingWhitespace_Trimmed()
    {
        var result = Normalization.SanitizeFileName("  report.xlsx  ");

        result.Should().Be("report.xlsx");
    }

    #endregion
}
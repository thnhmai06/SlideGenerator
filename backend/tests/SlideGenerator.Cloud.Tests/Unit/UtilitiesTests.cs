/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: UtilitiesTests.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Cloud.Application;
using Xunit;

namespace SlideGenerator.Cloud.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Utilities" />, verifying URI parsing, automatic scheme injection,
///     and graceful handling of empty or invalid inputs.
/// </summary>
public sealed class UtilitiesTests
{
    #region TryCreateUri(string?, out Uri)

    /// <summary>
    ///     Verifies that <see cref="Utilities.TryCreateUri(string?,out Uri)" /> returns
    ///     <see langword="true" /> and a valid <see cref="Uri" /> for a well-formed HTTPS URL.
    /// </summary>
    [Fact]
    public void TryCreateUri_ValidHttpsUrl_ReturnsTrueWithUri()
    {
        var result = Utilities.TryCreateUri("https://example.com/path", out var uri);

        result.Should().BeTrue();
        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Be("https://example.com/path");
    }

    /// <summary>
    ///     Verifies that a URL without a scheme gets <c>https://</c> prepended automatically,
    ///     and the resulting <see cref="Uri" /> is valid.
    /// </summary>
    [Fact]
    public void TryCreateUri_UrlWithoutScheme_PrependHttps()
    {
        var result = Utilities.TryCreateUri("drive.google.com/file/d/ABC", out var uri);

        result.Should().BeTrue();
        uri.Should().NotBeNull();
        uri.Scheme.Should().Be("https");
        uri.Host.Should().Be("drive.google.com");
    }

    /// <summary>
    ///     Verifies that an empty string returns <see langword="false" /> and a <see langword="null" /> URI.
    /// </summary>
    [Fact]
    public void TryCreateUri_EmptyString_ReturnsFalse()
    {
        var result = Utilities.TryCreateUri(string.Empty, out var uri);

        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that a <see langword="null" /> input returns <see langword="false" /> and a
    ///     <see langword="null" /> URI.
    /// </summary>
    [Fact]
    public void TryCreateUri_NullInput_ReturnsFalse()
    {
        var result = Utilities.TryCreateUri(null, out var uri);

        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that a whitespace-only string returns <see langword="false" />.
    /// </summary>
    [Fact]
    public void TryCreateUri_WhitespaceOnly_ReturnsFalse()
    {
        var result = Utilities.TryCreateUri("   ", out var uri);

        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    #endregion

    #region TryCreateUri(string?) overload

    /// <summary>
    ///     Verifies that the single-return overload returns a non-null <see cref="Uri" /> for
    ///     a valid URL.
    /// </summary>
    [Fact]
    public void TryCreateUri_Overload_ValidUrl_ReturnsUri()
    {
        var uri = Utilities.TryCreateUri("https://example.com");

        uri.Should().NotBeNull();
    }

    /// <summary>
    ///     Verifies that the single-return overload returns <see langword="null" /> for
    ///     an invalid input.
    /// </summary>
    [Fact]
    public void TryCreateUri_Overload_InvalidUrl_ReturnsNull()
    {
        var uri = Utilities.TryCreateUri(string.Empty);

        uri.Should().BeNull();
    }

    #endregion
}
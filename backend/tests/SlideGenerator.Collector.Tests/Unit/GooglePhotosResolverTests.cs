/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector.Tests
 * File: GooglePhotosResolverTests.cs
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
using NSubstitute;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Collector.Infrastructure.Resolvers;
using SlideGenerator.Collector.Tests.Unit.Helper;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Collector.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="GooglePhotosResolver" />, verifying URI support detection and
///     direct-link extraction from Google Photos HTML content.
/// </summary>
public sealed class GooglePhotosResolverTests
{
    private readonly GooglePhotosResolver _resolver = new(Substitute.For<ISystemLogger>());

    #region ResolveUriAsync — schema guard

    /// <summary>
    ///     Verifies that <see cref="GooglePhotosResolver.ResolveUriAsync" /> throws <see cref="ArgumentException" />
    ///     when called with a URI not belonging to Google Photos.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UnsupportedUri_ThrowsArgumentException()
    {
        var input = new Uri("https://drive.google.com/file/d/abc/view");
        using var httpClient = CollectorTestHelpers.CreateFakeHttpClient(string.Empty);

        var act = () => _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsUriSupported

    /// <summary>
    ///     Verifies that <see cref="GooglePhotosResolver.IsUriSupported" /> returns <see langword="true" />
    ///     with <see cref="CloudResolverKey.GooglePhotos" /> for all supported Google Photos host formats.
    /// </summary>
    [Theory]
    [InlineData("https://photos.app.goo.gl/abc123")]
    [InlineData("https://photos.google.com/photo/abc")]
    [InlineData("https://lh3.googleusercontent.com/pw/abc")]
    public void IsUriSupported_GooglePhotosUri_ReturnsTrueWithGooglePhotosKey(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudResolverKey.GooglePhotos);
    }

    /// <summary>
    ///     Verifies that <see cref="GooglePhotosResolver.IsUriSupported" /> returns <see langword="false" />
    ///     for URIs that do not belong to Google Photos.
    /// </summary>
    [Theory]
    [InlineData("https://drive.google.com/file/d/abc/view")]
    [InlineData("https://1drv.ms/b/abc")]
    [InlineData("https://example.com/photo.jpg")]
    public void IsUriSupported_NonGooglePhotosUri_ReturnsFalse(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveUriAsync — direct link extraction

    /// <summary>
    ///     Verifies that <see cref="GooglePhotosResolver.ResolveUriAsync" /> extracts the direct image URL
    ///     from Google Photos HTML and appends <c>=d</c> when no size modifier is present.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_HtmlContainsDirectUrl_ReturnsUrlWithRawQuality()
    {
        var input = new Uri("https://photos.app.goo.gl/abc123");
        const string fakeHtml =
            """<meta content="https://lh3.googleusercontent.com/pw/PHOTO_ID" property="og:image">""";
        using var httpClient = CollectorTestHelpers.CreateFakeHttpClient(fakeHtml);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.AbsoluteUri.Should().EndWith("=d");
    }

    /// <summary>
    ///     Verifies that <see cref="GooglePhotosResolver.ResolveUriAsync" /> returns the original URI unchanged
    ///     when no direct image URL can be found in the HTML content.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_HtmlHasNoDirectUrl_ReturnsOriginalUri()
    {
        var input = new Uri("https://photos.app.goo.gl/abc123");
        using var httpClient = CollectorTestHelpers.CreateFakeHttpClient("<html>no image here</html>");

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Should().Be(input);
    }

    #endregion
}


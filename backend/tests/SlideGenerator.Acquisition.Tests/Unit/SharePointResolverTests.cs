/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition.Tests
 * File: SharePointResolverTests.cs
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
using SlideGenerator.Acquisition.Domain.Models;
using SlideGenerator.Acquisition.Infrastructure.Resolvers;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Acquisition.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SharePointResolver" />, verifying URI support detection and the
///     construction of direct download links from SharePoint sharing URLs.
/// </summary>
public sealed class SharePointResolverTests
{
    private readonly SharePointResolver _resolver =
        new(Substitute.For<ISystemLogger>());

    #region IsUriSupported

    /// <summary>
    ///     Verifies that <see cref="SharePointResolver.IsUriSupported" /> returns <see langword="true" />
    ///     with a key of <see cref="CloudResolverKey.SharePoint" /> for URIs belonging to SharePoint.
    /// </summary>
    /// <param name="url">The SharePoint URI to be tested for support.</param>
    [Theory]
    [InlineData(
        "https://contoso.sharepoint.com/sites/shared/_layouts/15/Download.aspx?SourceUrl=/sites/shared/Documents/file.pptx")]
    [InlineData("https://company.sharepoint.com/:p:/g/personal/user/abc?e=xyz")]
    public void IsUriSupported_SharePointComUri_ReturnsTrueWithSharePointKey(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudResolverKey.SharePoint);
    }

    /// <summary>
    ///     Verifies that <see cref="SharePointResolver.IsUriSupported" /> returns <see langword="false" />
    ///     for URIs that do not belong to SharePoint.
    /// </summary>
    [Theory]
    [InlineData("https://drive.google.com/file/d/abc/view")]
    [InlineData("https://1drv.ms/b/abc")]
    [InlineData("https://example.com/report.pptx")]
    public void IsUriSupported_NonSharePointUri_ReturnsFalse(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveUriAsync

    /// <summary>
    ///     Verifies that <see cref="SharePointResolver.ResolveUriAsync" /> constructs a direct download
    ///     URI by extracting the <c>id</c> query parameter and appending <c>?download=1</c>.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UriWithIdParam_ReturnsDirectDownloadUri()
    {
        var input = new Uri(
            "https://contoso.sharepoint.com/_layouts/15/guestaccess.aspx?id=/sites/shared/Documents/slides.pptx&e=abc");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Host.Should().Be("contoso.sharepoint.com");
        result.Query.Should().Contain("download=1");
        result.AbsolutePath.Should().Be("/sites/shared/Documents/slides.pptx");
    }

    /// <summary>
    ///     Verifies that <see cref="SharePointResolver.ResolveUriAsync" /> returns the original URI unchanged
    ///     when the URI has no query string.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UriWithNoQueryString_ReturnsOriginalUri()
    {
        var input = new Uri("https://contoso.sharepoint.com/sites/shared/Documents/slides.pptx");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Should().Be(input);
    }

    /// <summary>
    ///     Verifies that <see cref="SharePointResolver.ResolveUriAsync" /> returns the original URI unchanged
    ///     when the query string does not contain an <c>id</c> parameter that starts with a forward slash.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_QueryWithoutIdParam_ReturnsOriginalUri()
    {
        var input = new Uri("https://contoso.sharepoint.com/sites/shared?e=token&other=val");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Should().Be(input);
    }

    /// <summary>
    ///     Verifies that <see cref="SharePointResolver.ResolveUriAsync" /> throws <see cref="ArgumentException" />
    ///     when called with a URI not belonging to SharePoint.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UnsupportedUri_ThrowsArgumentException()
    {
        var input = new Uri("https://drive.google.com/file/d/abc/view");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var act = () => _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
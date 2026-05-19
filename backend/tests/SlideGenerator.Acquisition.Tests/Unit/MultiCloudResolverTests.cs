/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition.Tests
 * File: MultiCloudResolverTests.cs
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
using SlideGenerator.Acquisition.Infrastructure.Services;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Acquisition.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="MultiCloudResolver" />, verifying that the composite resolver correctly
///     delegates URI support checks and resolution to the appropriate provider, and falls back gracefully
///     for unrecognized URIs.
/// </summary>
public sealed class MultiCloudResolverTests
{
    private readonly MultiCloudResolver _resolver =
        new(Substitute.For<ISystemLogger>());

    #region IsUriSupported

    /// <summary>
    ///     Verifies that <see cref="MultiCloudResolver.IsUriSupported" /> returns <see langword="true" />
    ///     for each supported cloud provider URI format, assigning the correct <see cref="CloudResolverKey" />.
    /// </summary>
    [Theory]
    [InlineData("https://drive.google.com/file/d/abc/view", CloudResolverKey.GoogleDrive)]
    [InlineData("https://photos.google.com/photo/abc", CloudResolverKey.GooglePhotos)]
    [InlineData("https://photos.app.goo.gl/abc", CloudResolverKey.GooglePhotos)]
    [InlineData("https://1drv.ms/b/abc", CloudResolverKey.OneDrive)]
    [InlineData("https://onedrive.live.com/redir?resid=abc", CloudResolverKey.OneDrive)]
    [InlineData("https://contoso.sharepoint.com/sites/doc", CloudResolverKey.SharePoint)]
    public void IsUriSupported_SupportedCloudUri_ReturnsTrueWithCorrectKey(string url, CloudResolverKey expectedKey)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(expectedKey);
    }

    /// <summary>
    ///     Verifies that <see cref="MultiCloudResolver.IsUriSupported" /> returns <see langword="false" />
    ///     for URIs that do not match any registered cloud provider.
    /// </summary>
    [Theory]
    [InlineData("https://example.com/report.pptx")]
    [InlineData("https://cdn.company.com/image.png")]
    [InlineData("https://github.com/user/repo/releases")]
    public void IsUriSupported_UnrecognizedUri_ReturnsFalse(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveUriAsync

    /// <summary>
    ///     Verifies that <see cref="MultiCloudResolver.ResolveUriAsync" /> returns the original URI unchanged
    ///     when it does not match any registered cloud provider, without performing any network requests.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UnrecognizedUri_ReturnsOriginalUri()
    {
        var input = new Uri("https://example.com/file.pptx");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Should().Be(input);
    }

    /// <summary>
    ///     Verifies that <see cref="MultiCloudResolver.ResolveUriAsync" /> produces a resolved URI
    ///     for a recognized OneDrive URI, confirming delegation to the <see cref="CloudResolverKey.OneDrive" />
    ///     resolver (which performs pure computation with no HTTP calls).
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_OneDriveUri_ReturnsResolvedApiUri()
    {
        var input = new Uri("https://1drv.ms/b/s!AKxxxxxx");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Host.Should().Be("api.onedrive.com");
        result.AbsolutePath.Should().StartWith("/v1.0/shares/");
    }

    /// <summary>
    ///     Verifies that <see cref="MultiCloudResolver.ResolveUriAsync" /> resolves a SharePoint URI with
    ///     an <c>id</c> parameter to a direct download link.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_SharePointUri_ReturnsDirectDownloadUri()
    {
        var input = new Uri(
            "https://contoso.sharepoint.com/_layouts/guestaccess.aspx?id=/sites/shared/report.pptx&e=tok");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Query.Should().Contain("download=1");
    }

    #endregion
}
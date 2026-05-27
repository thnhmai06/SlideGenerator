/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector.Tests
 * File: OneDriveResolverTests.cs
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
using System.Text;
using FluentAssertions;
using NSubstitute;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Collector.Infrastructure.Resolvers;
using SlideGenerator.Collector.Tests.Unit.Helper;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Collector.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="OneDriveResolver" />, verifying URI support detection and the
///     base64-encoded API URL generation used to construct direct OneDrive download links.
/// </summary>
public sealed class OneDriveResolverTests
{
    private readonly OneDriveResolver _resolver =
        new(Substitute.For<ISystemLogger>());

    #region IsUriSupported

    /// <summary>
    ///     Verifies that <see cref="OneDriveResolver.IsUriSupported" /> returns <see langword="true" />
    ///     for shortened <c>1drv.ms</c> link URIs and assigns the <see cref="CloudResolverKey.OneDrive" /> key.
    /// </summary>
    [Fact]
    public void IsUriSupported_OneDriveShortLink_ReturnsTrueWithOneDriveKey()
    {
        var uri = new Uri("https://1drv.ms/b/s!AKxxxxxx");

        var result = _resolver.IsUriSupported(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudResolverKey.OneDrive);
    }

    /// <summary>
    ///     Verifies that <see cref="OneDriveResolver.IsUriSupported" /> returns <see langword="true" />
    ///     for full <c>onedrive.live.com</c> sharing URIs.
    /// </summary>
    [Fact]
    public void IsUriSupported_OneDriveLiveComUri_ReturnsTrueWithOneDriveKey()
    {
        var uri = new Uri("https://onedrive.live.com/redir?resid=xxx");

        var result = _resolver.IsUriSupported(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudResolverKey.OneDrive);
    }

    /// <summary>
    ///     Verifies that <see cref="OneDriveResolver.IsUriSupported" /> returns <see langword="false" />
    ///     for URIs that do not belong to OneDrive.
    /// </summary>
    [Theory]
    [InlineData("https://drive.google.com/file/d/abc/view")]
    [InlineData("https://company.sharepoint.com/sites/shared")]
    [InlineData("https://example.com/file.pptx")]
    public void IsUriSupported_NonOneDriveUri_ReturnsFalse(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveUriAsync

    /// <summary>
    ///     Verifies that <see cref="OneDriveResolver.ResolveUriAsync" /> returns a URI pointing to the
    ///     OneDrive v1.0 API shares endpoint, constructed from a URL-safe base64 encoding of the input URI.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_AnyOneDriveUri_ReturnsApiSharesUri()
    {
        var input = new Uri("https://1drv.ms/b/s!AKxxxxxx");
        using var httpClient = CollectorTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Host.Should().Be("api.onedrive.com");
        result.AbsolutePath.Should().StartWith("/v1.0/shares/");
    }

    /// <summary>
    ///     Verifies that the encoded share token in the resolved URI uses the <c>u!</c> prefix and
    ///     applies URL-safe base64 encoding (no <c>+</c>, <c>/</c>, or trailing <c>=</c> characters).
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_AnyOneDriveUri_EncodedTokenIsUrlSafeBase64()
    {
        var input = new Uri("https://1drv.ms/b/s!AKxxxxxx");
        using var httpClient = CollectorTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        var segments = result.AbsolutePath.Split('/');
        var sharesIdx = Array.IndexOf(segments, "shares");
        var shareToken = segments[sharesIdx + 1];

        shareToken.Should().StartWith("u!");
        shareToken.Should().NotContain("+");
        shareToken.Should().NotContain("/");
        shareToken.Should().NotContain("=");
    }

    /// <summary>
    ///     Verifies that the encoded share token is derived from the input URI using the expected
    ///     base64 encoding algorithm: UTF-8 bytes → base64 → strip trailing '=' → replace '/' with '_',
    ///     '+' with '-', then prepend 'u!'.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_KnownUri_ProducesExpectedEncodedToken()
    {
        var input = new Uri("https://1drv.ms/b/s!AKxxxxxx");
        using var httpClient = CollectorTestHelpers.CreateFakeHttpClient(string.Empty);

        var rawBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(input.AbsoluteUri));
        var expectedToken = "u!" + rawBase64.TrimEnd('=').Replace('/', '_').Replace('+', '-');

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.AbsoluteUri.Should().Be($"https://api.onedrive.com/v1.0/shares/{expectedToken}/root/content");
    }

    /// <summary>
    ///     Verifies that <see cref="OneDriveResolver.ResolveUriAsync" /> throws <see cref="ArgumentException" />
    ///     when called with a URI not belonging to OneDrive.
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
}

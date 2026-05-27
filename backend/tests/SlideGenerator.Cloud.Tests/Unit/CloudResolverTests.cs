/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: CloudResolverTests.cs
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

using System.Net;
using FluentAssertions;
using SlideGenerator.Cloud.Domain.Models;
using SlideGenerator.Cloud.Infrastructure.Services;
using SlideGenerator.Cloud.Tests.Helpers;
using Xunit;

namespace SlideGenerator.Cloud.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="CloudResolver" />, verifying cloud host detection, URL parsing,
///     null-safety for invalid inputs, and routing of non-cloud URLs.
///     HTTP calls are intercepted by <see cref="FakeHttpHandler" />.
/// </summary>
public sealed class CloudResolverTests
{
    private readonly CloudResolver _sut = new(new CloudClient());

    #region GetCloudHost

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.GetCloudHost" /> returns <see langword="true" />
    ///     and sets key to <see cref="CloudHost.GoogleDrive" /> for a Google Drive URL.
    /// </summary>
    [Fact]
    public void GetCloudHost_GoogleDriveUrl_ReturnsTrueWithCorrectKey()
    {
        var result = _sut.GetCloudHost("https://drive.google.com/file/d/ABC/view", out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudHost.GoogleDrive);
    }

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.GetCloudHost" /> returns <see langword="false" />
    ///     for a URL that does not belong to any registered cloud provider.
    /// </summary>
    [Fact]
    public void GetCloudHost_NonCloudUrl_ReturnsFalse()
    {
        var result = _sut.GetCloudHost("https://example.com/image.jpg", out _);

        result.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.GetCloudHost" /> returns <see langword="false" />
    ///     for an empty or invalid URL without throwing.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("not a url at all!!!")]
    public void GetCloudHost_InvalidUrl_ReturnsFalse(string url)
    {
        var result = _sut.GetCloudHost(url, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveAsync

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.ResolveAsync" /> returns <see langword="null" />
    ///     for an empty string without throwing.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_EmptyUrl_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        var result = await _sut.ResolveAsync(string.Empty, cancellationToken: ct);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.ResolveAsync" /> returns <see langword="null" />
    ///     for a plaintext string that cannot be parsed as a URI.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_PlainTextNotUrl_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;

        var result = await _sut.ResolveAsync("This is not a URL, definitely", cancellationToken: ct);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudResolver.ResolveAsync" /> returns the final URI unchanged
    ///     for a non-cloud URL (no cloud module matches), after following any redirects.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_NonCloudUrl_ReturnsOriginalUri()
    {
        var ct = TestContext.Current.CancellationToken;
        const string url = "https://example.com/image.jpg";
        var handler = new FakeHttpHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = req
        });
        using var client = new HttpClient(handler);

        var result = await _sut.ResolveAsync(url, client, ct);

        result.Should().NotBeNull();
        result.Host.Should().Be("example.com");
    }

    /// <summary>
    ///     Verifies that a Google Drive file URL resolves to a <c>uc?export=download</c> URI,
    ///     confirming that <see cref="CloudResolver" /> delegates to <see cref="Infrastructure.Module.GoogleDriveModule" />
    ///     correctly.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_GoogleDriveFileUrl_ReturnsDownloadUri()
    {
        var ct = TestContext.Current.CancellationToken;
        const string fileId = "TESTFILE123";
        var url = $"https://drive.google.com/file/d/{fileId}/view?usp=sharing";
        var handler = new FakeHttpHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = req
        });
        using var client = new HttpClient(handler);

        var result = await _sut.ResolveAsync(url, client, ct);

        result.Should().NotBeNull();
        result.AbsoluteUri.Should().Be($"https://drive.google.com/uc?export=download&id={fileId}");
    }

    #endregion
}
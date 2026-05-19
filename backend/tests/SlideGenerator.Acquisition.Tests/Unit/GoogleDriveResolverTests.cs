/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition.Tests
 * File: GoogleDriveResolverTests.cs
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
///     Unit tests for <see cref="GoogleDriveResolver" />, verifying URI support detection and direct-link
///     resolution for the three supported Google Drive URL formats: file share, query-string id, and folder.
/// </summary>
public sealed class GoogleDriveResolverTests
{
    private readonly GoogleDriveResolver _resolver = new(Substitute.For<ISystemLogger>());

    #region ResolveUriAsync — /file/d/ format

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.ResolveUriAsync" /> extracts the file ID from a
    ///     <c>/file/d/{id}/view</c> share URL and returns the standard direct-download URI.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FileDFormat_ReturnsDirectDownloadUri()
    {
        var input = new Uri("https://drive.google.com/file/d/MYFILEID123/view?usp=sharing");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.AbsoluteUri.Should().Be("https://drive.google.com/uc?export=download&id=MYFILEID123");
    }

    #endregion

    #region ResolveUriAsync — ?id= query format

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.ResolveUriAsync" /> extracts the file ID from a
    ///     <c>?id=</c> query-string URL and returns the standard direct-download URI.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_QueryIdFormat_ReturnsDirectDownloadUri()
    {
        var input = new Uri("https://drive.google.com/open?id=QUERYFILEID");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.AbsoluteUri.Should().Be("https://drive.google.com/uc?export=download&id=QUERYFILEID");
    }

    #endregion

    #region ResolveUriAsync — schema guard

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.ResolveUriAsync" /> throws <see cref="ArgumentException" />
    ///     when called with a URI not belonging to Google Drive.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UnsupportedUri_ThrowsArgumentException()
    {
        var input = new Uri("https://photos.google.com/photo/abc");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(string.Empty);

        var act = () => _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsUriSupported

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.IsUriSupported" /> returns <see langword="true" />
    ///     with <see cref="CloudResolverKey.GoogleDrive" /> for URIs that belong to the supported
    ///     Google Drive formats, including file share, query-string id, and folder URLs.
    /// </summary>
    /// <param name="url">A string representing the Google Drive URI to test.</param>
    [Theory]
    [InlineData("https://drive.google.com/file/d/FILEID/view?usp=sharing")]
    [InlineData("https://drive.google.com/open?id=FILEID")]
    [InlineData("https://drive.google.com/drive/folders/FOLDERID")]
    public void IsUriSupported_DriveGoogleComUri_ReturnsTrueWithGoogleDriveKey(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudResolverKey.GoogleDrive);
    }

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.IsUriSupported" /> returns <see langword="false" />
    ///     for URIs that do not belong to Google Drive.
    /// </summary>
    [Theory]
    [InlineData("https://photos.google.com/photo/abc")]
    [InlineData("https://1drv.ms/b/abc")]
    [InlineData("https://example.com/file.pptx")]
    public void IsUriSupported_NonDriveUri_ReturnsFalse(string url)
    {
        var uri = new Uri(url);

        var result = _resolver.IsUriSupported(uri, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveUriAsync — /folders/ format (HTTP required)

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.ResolveUriAsync" /> fetches the folder page HTML
    ///     and extracts the file ID via regex when given a <c>/folders/</c> URI.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FolderUriWithFileIdInHtml_ReturnsDirectDownloadUri()
    {
        var input = new Uri("https://drive.google.com/drive/folders/FOLDERID");
        const string fakeHtml = """<a href="/file/d/EMBEDDED_ID/view">Download</a>""";
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient(fakeHtml);

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.AbsoluteUri.Should().Be("https://drive.google.com/uc?export=download&id=EMBEDDED_ID");
    }

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveResolver.ResolveUriAsync" /> returns the original URI unchanged
    ///     when the folder page HTML does not contain an extractable file ID.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FolderUriWithNoFileIdInHtml_ReturnsOriginalUri()
    {
        var input = new Uri("https://drive.google.com/drive/folders/FOLDERID");
        using var httpClient = AcquisitionTestHelpers.CreateFakeHttpClient("<html>No file ID here</html>");

        var result = await _resolver.ResolveUriAsync(input, httpClient, TestContext.Current.CancellationToken);

        result.Should().Be(input);
    }

    #endregion
}
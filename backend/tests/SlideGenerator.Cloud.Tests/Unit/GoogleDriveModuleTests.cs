/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: GoogleDriveModuleTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Net;
using FluentAssertions;
using SlideGenerator.Cloud.Domain.Models;
using SlideGenerator.Cloud.Infrastructure.Module;
using SlideGenerator.Cloud.Tests.Helpers;
using Xunit;

namespace SlideGenerator.Cloud.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="GoogleDriveModule" />, verifying URL recognition, file-ID
///     extraction from various link formats, folder HTML scanning, and null-safe failure paths.
///     All HTTP calls are intercepted by <see cref="FakeHttpHandler" />.
/// </summary>
public sealed class GoogleDriveModuleTests
{
    private readonly GoogleDriveModule _sut = new();

    #region ResolveAsync — unsupported URI

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveModule.ResolveAsync" /> throws
    ///     <see cref="ArgumentException" /> when called with a URI that is not a Google Drive link.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_NonSupportedUri_ThrowsArgumentException()
    {
        var ct = TestContext.Current.CancellationToken;
        var nonDriveUri = new Uri("https://photos.google.com/album/XYZ");
        using var client = new HttpClient();

        // ReSharper disable once AccessToDisposedClosure
        var act = async () => await _sut.ResolveAsync(nonDriveUri, client, ct);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsResolvable

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveModule.IsResolvable" /> returns <see langword="true" />
    ///     and sets key to <see cref="CloudHost.GoogleDrive" /> for any URI
    ///     whose host ends with <c>drive.google.com</c>.
    /// </summary>
    [Fact]
    public void IsResolvable_DriveGoogleComUrl_ReturnsTrueWithGoogleDriveKey()
    {
        var uri = new Uri("https://drive.google.com/file/d/ABC123/view");

        var result = _sut.IsResolvable(uri, out var key);

        result.Should().BeTrue();
        key.Should().Be(CloudHost.GoogleDrive);
    }

    /// <summary>
    ///     Verifies that <see cref="GoogleDriveModule.IsResolvable" /> returns <see langword="false" />
    ///     for a URI that does not belong to Google Drive.
    /// </summary>
    [Fact]
    public void IsResolvable_NonDriveUrl_ReturnsFalse()
    {
        var uri = new Uri("https://photos.google.com/album/ABC");

        var result = _sut.IsResolvable(uri, out _);

        result.Should().BeFalse();
    }

    #endregion

    #region ResolveAsync — file links

    /// <summary>
    ///     Verifies that a <c>/file/d/{id}</c> sharing link resolves to the correct
    ///     <c>uc?export=download&amp;id={id}</c> download URI without any HTTP request.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FileUrl_ReturnsDownloadUri()
    {
        var ct = TestContext.Current.CancellationToken;
        var fileUri = new Uri("https://drive.google.com/file/d/XYZ789/view?usp=sharing");
        using var client =
            new HttpClient(new FakeHttpHandler(_ => throw new InvalidOperationException("No HTTP call expected")));

        var result = await _sut.ResolveAsync(fileUri, client, ct);

        result.Should().NotBeNull();
        result.AbsoluteUri.Should().Be("https://drive.google.com/uc?export=download&id=XYZ789");
    }

    /// <summary>
    ///     Verifies that a <c>uc?id={id}</c> style URL resolves to the corresponding download URI.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_UcUrlWithIdParam_ReturnsDownloadUri()
    {
        var ct = TestContext.Current.CancellationToken;
        var ucUri = new Uri("https://drive.google.com/uc?id=ABCDEF&export=view");
        using var client =
            new HttpClient(new FakeHttpHandler(_ => throw new InvalidOperationException("No HTTP call expected")));

        var result = await _sut.ResolveAsync(ucUri, client, ct);

        result.Should().NotBeNull();
        result.AbsoluteUri.Should().Be("https://drive.google.com/uc?export=download&id=ABCDEF");
    }

    #endregion

    #region ResolveAsync — folder links

    /// <summary>
    ///     Verifies that a folder link resolves to the download URI of the first file found in the
    ///     embedded folder view HTML.
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FolderUrlWithFileInHtml_ReturnsFirstFileDownloadUri()
    {
        var ct = TestContext.Current.CancellationToken;
        const string folderId = "FOLDERID123";
        const string fileId = "FILEID456";
        var folderUri = new Uri($"https://drive.google.com/drive/folders/{folderId}?usp=sharing");
        var html = $"""<a href="/file/d/{fileId}/view?usp=drive_web">image.jpg</a>""";

        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        });
        using var client = new HttpClient(handler);

        var result = await _sut.ResolveAsync(folderUri, client, ct);

        result.Should().NotBeNull();
        result.AbsoluteUri.Should().Be($"https://drive.google.com/uc?export=download&id={fileId}");
    }

    /// <summary>
    ///     Verifies that a folder link returns <see langword="null" /> when the embedded folder view
    ///     HTML contains no <c>/file/d/</c> links (folder contains only subfolders or is empty).
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FolderUrlWithNoFileInHtml_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        const string folderId = "EMPTYFOLDERID";
        var folderUri = new Uri($"https://drive.google.com/drive/folders/{folderId}");
        var html = """<a href="/drive/folders/SUBFOLDER1">Sub Folder 1</a>""";

        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        });
        using var client = new HttpClient(handler);

        var result = await _sut.ResolveAsync(folderUri, client, ct);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that a folder link returns <see langword="null" /> when the HTTP request for the
    ///     embedded folder view fails (permission denied, network error, etc.).
    /// </summary>
    [Fact]
    public async Task ResolveUriAsync_FolderUrlHttpError_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var folderUri = new Uri("https://drive.google.com/drive/folders/RESTRICTED");
        var handler = new FakeHttpHandler(_ => throw new HttpRequestException("Forbidden"));
        using var client = new HttpClient(handler);

        var result = await _sut.ResolveAsync(folderUri, client, ct);

        result.Should().BeNull();
    }

    #endregion
}
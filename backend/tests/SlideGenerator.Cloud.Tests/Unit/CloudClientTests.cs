/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: CloudClientTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SlideGenerator.Cloud.Services;
using SlideGenerator.Cloud.Tests.Helpers;
using Xunit;

namespace SlideGenerator.Cloud.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="CloudClient" />, verifying redirect resolution, content-type
///     detection, content-length extraction, failure handling, and file download behavior.
///     All network interaction is provided by <see cref="FakeHttpHandler" />.
/// </summary>
public sealed class CloudClientTests : IDisposable
{
    private static readonly Uri TestUri = new("https://example.com/resource");
    private readonly CloudClient _sut = new();
    private string? _tempFile;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_tempFile is not null && File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    #region DownloadAsync

    /// <summary>
    ///     Verifies that <see cref="CloudClient.DownloadAsync" /> writes the response body bytes
    ///     to the specified file path exactly.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_ValidUri_WritesContentToFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var expectedBytes = "fake binary content"u8.ToArray();
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedBytes)
        });
        using var client = new HttpClient(handler);
        _tempFile = Path.GetTempFileName();

        await _sut.DownloadAsync(TestUri, _tempFile, client, ct);

        var writtenBytes = await File.ReadAllBytesAsync(_tempFile, ct);
        writtenBytes.Should().Equal(expectedBytes);
    }

    /// <summary>
    ///     Verifies that the byte-array overload of <see cref="CloudClient.DownloadAsync" /> returns
    ///     the response body directly in memory, without touching disk.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_ByteArrayOverload_ReturnsContentBytes()
    {
        var ct = TestContext.Current.CancellationToken;
        var expectedBytes = "fake binary content"u8.ToArray();
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedBytes)
        });
        using var client = new HttpClient(handler);

        var result = await _sut.DownloadAsync(TestUri, client, ct);

        result.Should().Equal(expectedBytes);
    }

    #endregion

    #region InspectAsync

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns a <c>ContentInfo</c> whose
    ///     <c>IsImage()</c> is <see langword="true" /> when the HEAD response carries an image MIME type.
    /// </summary>
    [Fact]
    public async Task InspectAsync_ImageContentType_ReturnsContentInfoWithIsImageTrue()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().NotBeNull();
        result.IsImage().Should().BeTrue();
        result.MimeType.Should().Be("image/jpeg");
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns a <c>ContentInfo</c> whose
    ///     <c>IsImage()</c> is <see langword="false" /> for a non-image MIME type.
    /// </summary>
    [Fact]
    public async Task InspectAsync_NonImageContentType_ReturnsIsImageFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().NotBeNull();
        result.IsImage().Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> captures the <c>Content-Length</c>
    ///     header value in the returned <c>ContentInfo</c>.
    /// </summary>
    [Fact]
    public async Task InspectAsync_WithContentLength_ReturnsCorrectLength()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            r.Content.Headers.ContentLength = 4096;
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().NotBeNull();
        result.Length.Should().Be(4096u);
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> stores the final URI after a
    ///     redirect in <c>ContentInfo.Uri</c>, not the originally requested URI.
    /// </summary>
    [Fact]
    public async Task InspectAsync_WithRedirect_ReturnsFinalUriInContentInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        var finalUri = new Uri("https://cdn.example.com/final-image.jpg");
        var handler = new FakeHttpHandler(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            r.RequestMessage = new HttpRequestMessage(HttpMethod.Head, finalUri);
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().NotBeNull();
        result.Uri.Should().Be(finalUri);
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns <see langword="null" />
    ///     when the HTTP request throws an exception (network error, timeout, etc.).
    /// </summary>
    [Fact]
    public async Task InspectAsync_HttpException_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ => throw new HttpRequestException("Network error"));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> creates its own <see cref="HttpClient" />
    ///     when <see langword="null" /> is passed and returns <see langword="null" /> gracefully for an
    ///     unreachable host (instead of throwing).
    /// </summary>
    [Fact]
    public async Task InspectAsync_NullHttpClient_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;

        var result = await _sut.InspectAsync(new Uri("https://this-host-does-not-exist.invalid"), null, ct);

        result.Should().BeNull();
    }

    #endregion

    #region InspectAsync — Extension

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> takes <c>Extension</c> from the
    ///     <c>Content-Disposition</c> file name when present, in preference to the URL path.
    /// </summary>
    [Fact]
    public async Task InspectAsync_ContentDispositionFileName_ReturnsExtensionFromFileName()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            r.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = "photo.png" };
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(new Uri("https://example.com/download?id=1"), client, ct);

        result.Should().NotBeNull();
        result.Extension.Should().Be(".png");
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> falls back to the URL path's extension
    ///     when no <c>Content-Disposition</c> file name is present.
    /// </summary>
    [Fact]
    public async Task InspectAsync_NoContentDisposition_ReturnsExtensionFromUrlPath()
    {
        var ct = TestContext.Current.CancellationToken;
        var uri = new Uri("https://example.com/images/photo.jpg");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(uri, client, ct);

        result.Should().NotBeNull();
        result.Extension.Should().Be(".jpg");
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns a <see langword="null" />
    ///     <c>Extension</c> when neither the <c>Content-Disposition</c> file name nor the URL path
    ///     carries one.
    /// </summary>
    [Fact]
    public async Task InspectAsync_NoFileNameOrUrlExtension_ReturnsNullExtension()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().NotBeNull();
        result.Extension.Should().BeNull();
    }

    #endregion

    #region InspectAsync — Cloud resolution

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> resolves a Google Drive file sharing
    ///     link to a <c>uc?export=download</c> download URI and stores it in <c>ContentInfo.Uri</c>.
    /// </summary>
    [Fact]
    public async Task InspectAsync_GoogleDriveFileUrl_ContentInfoUriIsDownloadUri()
    {
        var ct = TestContext.Current.CancellationToken;
        const string fileId = "TESTFILE123";
        var driveUri = new Uri($"https://drive.google.com/file/d/{fileId}/view");
        var handler = new FakeHttpHandler(req =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            if (req.RequestUri?.AbsoluteUri.Contains("export=download") == true)
                r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(driveUri, client, ct);

        result.Should().NotBeNull();
        result.Uri.AbsoluteUri.Should().Be($"https://drive.google.com/uc?export=download&id={fileId}");
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> re-inspects the resolved download URI
    ///     so that <c>IsImage()</c> returns <see langword="true" /> for a Google Drive image file.
    /// </summary>
    [Fact]
    public async Task InspectAsync_GoogleDriveFileUrl_IsImageTrueAfterReInspect()
    {
        var ct = TestContext.Current.CancellationToken;
        const string fileId = "TESTFILE123";
        var driveUri = new Uri($"https://drive.google.com/file/d/{fileId}/view");
        var handler = new FakeHttpHandler(req =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK);
            if (req.RequestUri?.AbsoluteUri.Contains("export=download") == true)
                r.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return r;
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(driveUri, client, ct);

        result.Should().NotBeNull();
        result.IsImage().Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns the final redirect URI
    ///     unchanged when no cloud provider module matches.
    /// </summary>
    [Fact]
    public async Task InspectAsync_NonCloudUrl_ContentInfoUriIsUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var uri = new Uri("https://example.com/image.jpg");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(uri, client, ct);

        result.Should().NotBeNull();
        result.Uri.Should().Be(uri);
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns <c>ContentInfo</c> with the
    ///     folder URI when the Google Drive module cannot find a file in the folder (returns
    ///     <see langword="null" />).
    /// </summary>
    [Fact]
    public async Task InspectAsync_GoogleDriveModuleReturnsNull_ContentInfoUriIsFinalUri()
    {
        var ct = TestContext.Current.CancellationToken;
        const string folderId = "EMPTYFOLDERID";
        var folderUri = new Uri($"https://drive.google.com/drive/folders/{folderId}");
        var handler = new FakeHttpHandler(req =>
        {
            if (req.RequestUri?.AbsoluteUri.Contains("embeddedfolderview") == true)
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent("<html>no files here</html>") };
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(folderUri, client, ct);

        result.Should().NotBeNull();
        result.Uri.Should().Be(folderUri);
    }

    #endregion
}
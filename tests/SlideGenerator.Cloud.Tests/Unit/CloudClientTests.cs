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
using System.Text;
using FluentAssertions;
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
    private readonly string _tempDirectory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "SlideGenerator.Cloud.Tests", Guid.NewGuid().ToString("N"))).FullName;
    private string? _tempFile;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_tempFile is not null && File.Exists(_tempFile))
            File.Delete(_tempFile);

        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    /// <summary>
    ///     Builds a method-specific factory map whose <see cref="HttpMethod.Head" /> entry is
    ///     <paramref name="head" />; every other method is answered by the handler's fallback factory.
    /// </summary>
    private static IReadOnlyDictionary<HttpMethod, Func<HttpRequestMessage, HttpResponseMessage>> HeadResponds(
        Func<HttpRequestMessage, HttpResponseMessage> head)
    {
        return new Dictionary<HttpMethod, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            [HttpMethod.Head] = head
        };
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
    ///     the response body directly in memory, without touching the disk.
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

    #region InspectAsync — HEAD fallback

    /// <summary>
    ///     Verifies that a HEAD answered with 403 falls back to a header-only GET and still yields
    ///     content metadata.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadForbidden_FallsBackToGetAndReturnsInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await AssertHeadFallsBackToGet(_ => new HttpResponseMessage(HttpStatusCode.Forbidden), ct);
    }

    /// <summary>
    ///     Verifies that a HEAD answered with 501 falls back to a header-only GET and still yields
    ///     content metadata.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadNotImplemented_FallsBackToGetAndReturnsInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await AssertHeadFallsBackToGet(_ => new HttpResponseMessage(HttpStatusCode.NotImplemented), ct);
    }

    /// <summary>
    ///     Verifies that a HEAD answered with 404 falls back to a header-only GET and still yields
    ///     content metadata.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadNotFound_FallsBackToGetAndReturnsInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await AssertHeadFallsBackToGet(_ => new HttpResponseMessage(HttpStatusCode.NotFound), ct);
    }

    /// <summary>
    ///     Verifies that a HEAD that throws (e.g. the server drops the connection) falls back to a
    ///     header-only GET and still yields content metadata.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadThrows_FallsBackToGetAndReturnsInfo()
    {
        var ct = TestContext.Current.CancellationToken;
        await AssertHeadFallsBackToGet(_ => throw new HttpRequestException("HEAD dropped"), ct);
    }

    /// <summary>
    ///     Verifies that a successful HEAD is not followed by a GET request.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadSucceeds_DoesNotSendGet()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("image-bytes", Encoding.UTF8, "image/jpeg")
        });
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().NotBeNull();
        result.IsImage().Should().BeTrue();
        handler.Methods.Should().NotContain(HttpMethod.Get);
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns <see langword="null" />
    ///     when both the HEAD and the fallback GET answer with a non-success status.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadAndGetNotFound_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="CloudClient.InspectAsync" /> returns <see langword="null" />
    ///     when both the HEAD and the fallback GET throw.
    /// </summary>
    [Fact]
    public async Task InspectAsync_HeadAndGetThrow_ReturnsNull()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ => throw new HttpRequestException("Network error"));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, ct);

        result.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that a cancelled token is not swallowed into a <see langword="null" /> result but
    ///     propagates as <see cref="OperationCanceledException" />.
    /// </summary>
    [Fact]
    public async Task InspectAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var ct = cts.Token;
        var handler = new FakeHttpHandler(_ =>
        {
            ct.ThrowIfCancellationRequested();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);

        var act = () => _sut.InspectAsync(TestUri, client, ct);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    ///     Registers a HEAD factory and asserts that inspection falls back to a GET, producing an
    ///     image <c>ContentInfo</c>.
    /// </summary>
    private async Task AssertHeadFallsBackToGet(
        Func<HttpRequestMessage, HttpResponseMessage> head,
        CancellationToken cancellationToken)
    {
        var handler = new FakeHttpHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("image-bytes", Encoding.UTF8, "image/jpeg")
            },
            HeadResponds(head));
        using var client = new HttpClient(handler);

        var result = await _sut.InspectAsync(TestUri, client, cancellationToken);

        result.Should().NotBeNull();
        result.IsImage().Should().BeTrue();
        handler.Methods.Should().Contain(HttpMethod.Head);
        handler.Methods.Should().Contain(HttpMethod.Get);
    }

    #endregion

    #region InspectAsync — client lifetime

    /// <summary>
    ///     Verifies that a client supplied by the caller is not disposed by
    ///     <see cref="CloudClient.InspectAsync" />: a subsequent request through the same client
    ///     still succeeds.
    /// </summary>
    [Fact]
    public async Task InspectAsync_ClientSuppliedByCaller_IsNotDisposed()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("image-bytes", Encoding.UTF8, "image/jpeg")
        });
        using var client = new HttpClient(handler);

        await _sut.InspectAsync(TestUri, client, ct);

        using var request = new HttpRequestMessage(HttpMethod.Get, TestUri);
        using var response = await client.SendAsync(request, ct);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region DownloadToFileAsync

    /// <summary>
    ///     Verifies that <see cref="CloudClient.DownloadToFileAsync" /> writes the response body to
    ///     the target file byte for byte.
    /// </summary>
    [Fact]
    public async Task DownloadToFileAsync_Success_WritesTheBodyToTheFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var expectedBytes = "streamed content"u8.ToArray();
        var savePath = Path.Combine(_tempDirectory, "success.bin");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedBytes)
        });
        using var client = new HttpClient(handler);

        await _sut.DownloadToFileAsync(TestUri, savePath, 0, client, ct);

        var writtenBytes = await File.ReadAllBytesAsync(savePath, ct);
        writtenBytes.Should().Equal(expectedBytes);
    }

    /// <summary>
    ///     Verifies that a declared <c>Content-Length</c> above the limit is rejected before the
    ///     output file is created.
    /// </summary>
    [Fact]
    public async Task DownloadToFileAsync_ContentLengthOverLimit_ThrowsBeforeWritingAnyFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var savePath = Path.Combine(_tempDirectory, "too-large.bin");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[64])
        });
        using var client = new HttpClient(handler);

        var act = () => _sut.DownloadToFileAsync(TestUri, savePath, 8, client, ct);

        await act.Should().ThrowAsync<InvalidDataException>();
        File.Exists(savePath).Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that when no <c>Content-Length</c> is declared and the streamed body grows past
    ///     the limit, the download is aborted and the partial file is deleted.
    /// </summary>
    [Fact]
    public async Task DownloadToFileAsync_BodyOverLimitWithoutContentLength_ThrowsAndDeletesPartialFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var savePath = Path.Combine(_tempDirectory, "streamed-too-large.bin");
        var content = new StreamContent(new MemoryStream(new byte[64]));
        content.Headers.ContentLength = null;
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        using var client = new HttpClient(handler);

        var act = () => _sut.DownloadToFileAsync(TestUri, savePath, 8, client, ct);

        await act.Should().ThrowAsync<InvalidDataException>();
        File.Exists(savePath).Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that a limit of zero means unlimited: the whole body is written to the file.
    /// </summary>
    [Fact]
    public async Task DownloadToFileAsync_LimitZero_IsUnlimited()
    {
        var ct = TestContext.Current.CancellationToken;
        var expectedBytes = new byte[256];
        Random.Shared.NextBytes(expectedBytes);
        var savePath = Path.Combine(_tempDirectory, "unlimited.bin");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedBytes)
        });
        using var client = new HttpClient(handler);

        await _sut.DownloadToFileAsync(TestUri, savePath, 0, client, ct);

        var writtenBytes = await File.ReadAllBytesAsync(savePath, ct);
        writtenBytes.Should().Equal(expectedBytes);
    }

    /// <summary>
    ///     Verifies that an HTTP error status surfaces as an exception and leaves no file behind.
    /// </summary>
    [Fact]
    public async Task DownloadToFileAsync_HttpErrorStatus_ThrowsAndLeavesNoFile()
    {
        var ct = TestContext.Current.CancellationToken;
        var savePath = Path.Combine(_tempDirectory, "missing.bin");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });
        using var client = new HttpClient(handler);

        var act = () => _sut.DownloadToFileAsync(TestUri, savePath, 0, client, ct);

        await act.Should().ThrowAsync<HttpRequestException>();
        File.Exists(savePath).Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that a cancelled token aborts the download and leaves no file behind.
    /// </summary>
    [Fact]
    public async Task DownloadToFileAsync_CancelledToken_LeavesNoFile()
    {
        var savePath = Path.Combine(_tempDirectory, "cancelled.bin");
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[16])
        });
        using var client = new HttpClient(handler);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _sut.DownloadToFileAsync(TestUri, savePath, 0, client, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        File.Exists(savePath).Should().BeFalse();
    }

    #endregion
}
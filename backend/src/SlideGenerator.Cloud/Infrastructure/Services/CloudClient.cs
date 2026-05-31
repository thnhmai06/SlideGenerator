/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: CloudClient.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Net;
using Microsoft.Extensions.Logging;
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Cloud.Domain.Models;

namespace SlideGenerator.Cloud.Infrastructure.Services;

/// <summary>
///     HTTP client facade that follows redirects, inspects resource metadata, and downloads files.
///     All methods accept an optional <see cref="HttpClient" />; a new auto-redirect instance is
///     created automatically when <see langword="null" /> is supplied.
/// </summary>
internal sealed class CloudClient(ILogger<CloudClient>? logger = null) : ICloudClient
{
    /// <inheritdoc />
    /// <remarks>
    ///     Sends a HEAD request first; if the server responds with 405 MethodNotAllowed, falls back
    ///     to a GET request with <see cref="HttpCompletionOption.ResponseHeadersRead" /> so that the
    ///     response body is never downloaded.  Because auto-redirect is enabled, the underlying
    ///     handler transparently follows redirects and the final destination URI is read from
    ///     <c>response.RequestMessage.RequestUri</c>.  Content-type and content-length are taken
    ///     from the response headers.  Any exception (network, timeout, DNS) is caught and
    ///     <see langword="null" /> is returned.
    /// </remarks>
    public async Task<ContentInfo?> InspectAsync(
        Uri uri,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= CreateClient();
        logger?.LogDebug("HTTP inspect start | Uri: {Uri}", uri);

        try
        {
            var headResp = await httpClient
                .SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), cancellationToken)
                .ConfigureAwait(false);

            HttpResponseMessage response;
            if (headResp.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                headResp.Dispose();
                response = await httpClient
                    .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                response = headResp;
            }

            using (response)
            {
                var finalUri = response.RequestMessage?.RequestUri ?? uri;
                var type = response.Content.Headers.ContentType?.MediaType;
                var rawLength = response.Content.Headers.ContentLength;
                var length = rawLength is > 0 ? (uint)rawLength.Value : (uint?)null;

                logger?.LogDebug("HTTP inspect completed | FinalUri: {FinalUri}, ContentType: {ContentType}", finalUri,
                    type);
                return new ContentInfo(finalUri, type, length);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "HTTP inspect failed, skipped | Uri: {Uri}", uri);
            return null;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Streams the response body directly to <paramref name="savePath" />, creating or
    ///     overwriting the file.  The caller is responsible for ensuring the directory exists.
    /// </remarks>
    public async Task DownloadAsync(
        Uri uri,
        string savePath,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= CreateClient();
        logger?.LogDebug("Download start | Uri: {Uri}, Path: {Path}", uri, savePath);

        await using var stream = await httpClient
            .GetStreamAsync(uri, cancellationToken)
            .ConfigureAwait(false);
        await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

        logger?.LogDebug("Download completed | Uri: {Uri}, Path: {Path}", uri, savePath);
    }

    /// <summary>Creates a new <see cref="HttpClient" /> with auto-redirect enabled (the default).</summary>
    private static HttpClient CreateClient()
    {
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
    }
}
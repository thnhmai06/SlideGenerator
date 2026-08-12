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
using SlideGenerator.Cloud.Abstractions;
using SlideGenerator.Cloud.Models;

namespace SlideGenerator.Cloud.Services;

/// <summary>
///     HTTP client facade that follows redirects, resolves cloud provider sharing links,
///     inspects resource metadata, and downloads files.
///     All methods accept an optional <see cref="HttpClient" />; a new auto-redirect instance is
///     created automatically when <see langword="null" /> is supplied.
/// </summary>
internal sealed class CloudClient(ILogger<CloudClient>? logger = null) : ICloudClient
{
    private readonly IReadOnlyList<CloudResolver> _resolvers = [new GoogleDriveModule()];

    /// <inheritdoc />
    /// <remarks>
    ///     Execution flow:
    ///     <list type="number">
    ///         <item>
    ///             Sends a HEAD request (falling back to GET on 405) and follows any redirects to get
    ///             the final URI, content-type, and content-length.
    ///         </item>
    ///         <item>
    ///             Checks whether the final URI is handled by a registered cloud provider module
    ///             (e.g., Google Drive).  If no module matches, returns the <see cref="ContentInfo" />
    ///             from the first request unchanged.
    ///         </item>
    ///         <item>
    ///             When a module matches, delegates to it to produce a direct download URI.
    ///             If the module returns <see langword="null" /> (e.g., empty folder, inaccessible
    ///             resource), the original final URI is returned unchanged.
    ///         </item>
    ///         <item>
    ///             Re-inspects the resolved download URI so that the returned
    ///             <see cref="ContentInfo" /> reflects the actual content-type and content-length of
    ///             the downloadable resource (not the sharing-page HTML).
    ///         </item>
    ///     </list>
    ///     Returns <see langword="null" /> only when the initial HTTP request fails entirely
    ///     (network error, timeout, DNS failure).
    /// </remarks>
    public async Task<ContentInfo?> InspectAsync(
        Uri uri,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= DefaultClient();
        logger?.LogDebug("HTTP inspect start | Uri: {Uri}", uri);

        // First Collect
        var info = await CollectContentInfo(uri, httpClient, cancellationToken).ConfigureAwait(false);
        if (info is null)
        {
            logger?.LogWarning("HTTP inspect failed, skipped | Uri: {Uri}", uri);
            return null;
        }

        // Resolve
        var finalUri = info.Uri;
        var resolver = FindResolver(finalUri);
        if (resolver is null)
        {
            logger?.LogDebug("No cloud provider matched, return direct URI | Uri: {Uri}", finalUri);
            return info;
        }

        logger?.LogDebug("Cloud provider matched | Uri: {Uri}", finalUri);

        Uri? resolvedUri;
        try
        {
            resolvedUri = await resolver.ResolveAsync(finalUri, httpClient, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Cloud module resolve failed, returning final URI | Uri: {Uri}", finalUri);
            return info;
        }

        if (resolvedUri is null)
        {
            logger?.LogDebug("Cloud module returned null, return final URI | Uri: {Uri}", finalUri);
            return info;
        }

        // Second Collect
        logger?.LogDebug("Cloud resolve completed, re-inspecting | ResolvedUri: {ResolvedUri}", resolvedUri);
        return await CollectContentInfo(resolvedUri, httpClient, cancellationToken).ConfigureAwait(false);
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
        savePath = Path.GetFullPath(savePath);
        httpClient ??= DefaultClient();
        logger?.LogDebug("Download start | Uri: {Uri}, Path: {Path}", uri, savePath);

        await using var stream = await httpClient
            .GetStreamAsync(uri, cancellationToken)
            .ConfigureAwait(false);
        await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

        logger?.LogDebug("Download completed | Uri: {Uri}, Path: {Path}", uri, savePath);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Streams the response body into an in-memory buffer — no file is created.
    /// </remarks>
    public async Task<byte[]> DownloadAsync(
        Uri uri,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= DefaultClient();
        logger?.LogDebug("Download start (in-memory) | Uri: {Uri}", uri);

        var bytes = await httpClient.GetByteArrayAsync(uri, cancellationToken).ConfigureAwait(false);

        logger?.LogDebug("Download completed (in-memory) | Uri: {Uri}", uri);
        return bytes;
    }

    #region Private helpers

    /// <summary>
    ///     Sends a HEAD request to <paramref name="uri" /> (falling back to GET on 405) and returns
    ///     a <see cref="ContentInfo" /> with the final URI, content-type, and content-length.
    ///     Returns <see langword="null" /> on any exception.
    /// </summary>
    private static async Task<ContentInfo?> CollectContentInfo(
        Uri uri,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        try
        {
            // HEAD
            var headResp = await httpClient
                .SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), cancellationToken)
                .ConfigureAwait(false);

            // GET fallback
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
                var mimeType = response.Content.Headers.ContentType?.MediaType;
                var rawLength = response.Content.Headers.ContentLength;
                var length = rawLength is > 0 ? (uint)rawLength.Value : (uint?)null;
                var extension = ExtractExtension(response, finalUri);
                return new ContentInfo(finalUri, mimeType, length, extension);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Extracts the file extension (with leading dot) from the response's <c>Content-Disposition</c>
    ///     file name, falling back to <paramref name="finalUri" />'s path when absent. Returns
    ///     <see langword="null" /> when neither source yields an extension.
    /// </summary>
    private static string? ExtractExtension(HttpResponseMessage response, Uri finalUri)
    {
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ??
                       response.Content.Headers.ContentDisposition?.FileName;
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var trimmed = fileName.Trim('"');
            var extFromFileName = Path.GetExtension(trimmed);
            if (!string.IsNullOrEmpty(extFromFileName)) return extFromFileName;
        }

        var extFromUrl = Path.GetExtension(finalUri.AbsolutePath);
        return string.IsNullOrEmpty(extFromUrl) ? null : extFromUrl;
    }

    /// <summary>Returns the first registered module that can handle <paramref name="uri" />,
    /// or <see langword="null" /> when none matches.</summary>
    private CloudResolver? FindResolver(Uri uri)
    {
        return _resolvers.FirstOrDefault(module => module.IsResolvable(uri));
    }

    /// <summary>Creates a new <see cref="HttpClient" /> with auto-redirect enabled (the default).</summary>
    private static HttpClient DefaultClient() => new(new HttpClientHandler { AllowAutoRedirect = true });

    #endregion
}
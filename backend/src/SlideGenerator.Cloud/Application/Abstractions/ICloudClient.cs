/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: ICloudClient.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Cloud.Domain.Models;

namespace SlideGenerator.Cloud.Application.Abstractions;

/// <summary>
///     Performs HTTP-based cloud resource operations: content inspection and file download.
/// </summary>
public interface ICloudClient
{
    /// <summary>
    ///     Resolves <paramref name="uri" /> to a direct download URI through a three-stage pipeline
    ///     and returns a <see cref="ContentInfo" /> record with the final URI, content-type, and
    ///     content-length.
    ///     <list type="number">
    ///         <item>
    ///             <b>HTTP redirect.</b> Sends HEAD (falling back to GET on 405) and follows any
    ///             redirects to get the final URI.
    ///         </item>
    ///         <item>
    ///             <b>Cloud resolution.</b> If the final URI is recognized by a registered cloud
    ///             provider module (e.g., Google Drive), delegates to that module to produce a direct
    ///             download URI.  When the module returns <see langword="null" /> (e.g., empty folder,
    ///             inaccessible resource), the stage-1 URI is kept unchanged.
    ///         </item>
    ///         <item>
    ///             <b>Re-inspection.</b> Sends a second HEAD/GET to the resolved download URI so that
    ///             the returned <see cref="ContentInfo" /> reflects the actual content-type of the
    ///             downloadable resource rather than the sharing-page HTML.
    ///         </item>
    ///     </list>
    ///     Returns <see langword="null" /> only when the initial HTTP request fails entirely
    ///     (network error, timeout, DNS failure).
    /// </summary>
    /// <param name="uri">The starting URI to inspect.</param>
    /// <param name="httpClient">
    ///     HTTP client used for all requests in the pipeline.  When <see langword="null" />, a new
    ///     instance is created automatically with redirect-following enabled.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<ContentInfo?> InspectAsync(
        Uri uri,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads the resource at <paramref name="uri" /> and writes it to <paramref name="savePath" />.
    ///     When <paramref name="httpClient" /> is <see langword="null" />, a new instance is created
    ///     automatically with redirect-following enabled.
    /// </summary>
    Task DownloadAsync(
        Uri uri,
        string savePath,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads the resource at <paramref name="uri" /> and returns its content directly as a
    ///     byte array, without writing to disk. When <paramref name="httpClient" /> is
    ///     <see langword="null" />, a new instance is created automatically with redirect-following
    ///     enabled.
    /// </summary>
    Task<byte[]> DownloadAsync(
        Uri uri,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default);
}
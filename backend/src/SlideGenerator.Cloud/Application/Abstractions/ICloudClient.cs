/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: ICloudClient.cs
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

using SlideGenerator.Cloud.Domain.Models;

namespace SlideGenerator.Cloud.Application.Abstractions;

/// <summary>
///     Performs HTTP-based cloud resource operations: content inspection and file download.
/// </summary>
public interface ICloudClient
{
    /// <summary>
    ///     Follows any HTTP redirects from <paramref name="uri" />, then sends a HEAD request to the
    ///     final destination, and returns a <see cref="ContentInfo" /> record containing the resolved
    ///     URI, content-type, and content-length.
    ///     Returns <see langword="null" /> when the request fails or the URI is unreachable.
    ///     When <paramref name="httpClient" /> is <see langword="null" />, a new instance is created
    ///     automatically with redirect-following enabled.
    /// </summary>
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
}
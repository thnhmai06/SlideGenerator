/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: ICloudResolver.cs
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
///     Resolves cloud provider sharing links to final direct download URIs.
/// </summary>
public interface ICloudResolver
{
    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="url" /> is recognized by a registered
    ///     cloud provider module, and sets <paramref name="key" /> to the matching provider.
    ///     Automatically prepends <c>https://</c> when <paramref name="url" /> contains no scheme.
    /// </summary>
    bool GetCloudHost(string url, out CloudHost key);

    /// <summary>
    ///     Resolves <paramref name="url" /> to a direct download <see cref="Uri" /> via the matching
    ///     cloud provider module, following any HTTP redirects first so that short links
    ///     (e.g. <c>bit.ly/…</c>) are expanded before provider matching.
    ///     Returns <see langword="null" /> when the URL cannot be parsed, the scheme is not
    ///     HTTP/HTTPS, or the provider module cannot produce a download URI (e.g., permission denied,
    ///     empty folder, non-existent resource).
    ///     For non-cloud URLs, returns the final URI after the redirect resolution is unchanged.
    ///     Automatically prepends <c>https://</c> when <paramref name="url" /> contains no scheme.
    ///     When <paramref name="httpClient" /> is <see langword="null" />, a new instance is created
    ///     automatically with redirect-following enabled.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown when the parsed URI uses an unsupported scheme (not HTTP or HTTPS).
    /// </exception>
    Task<Uri?> ResolveAsync(
        string url,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default);
}
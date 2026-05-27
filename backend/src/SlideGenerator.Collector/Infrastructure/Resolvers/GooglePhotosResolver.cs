/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector
 * File: GooglePhotosResolver.cs
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
using System.Text.RegularExpressions;
using SlideGenerator.Collector.Application.Abstractions;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Collector.Infrastructure.Resolvers;

/// <summary>
///     Provides access to Google Photos as a cloud provider, resolving album URLs to direct image links.
/// </summary>
internal sealed partial class GooglePhotosResolver(ISystemLogger logger) : ICloudResolver
{
    /// <summary>
    ///     A compiled regular expression for extracting direct image URLs from Google Photos HTML content.
    /// </summary>
    private static readonly Regex GooglePhotosUrlPattern = GooglePhotosUrlRegex();

    /// <inheritdoc />
    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        key = CloudResolverKey.GooglePhotos;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host;
        return host.EndsWith("photos.app.goo.gl", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("photos.google.com", StringComparison.OrdinalIgnoreCase) ||
               host.Contains("googleusercontent.com");
    }

    /// <inheritdoc />
    public async Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (!IsUriSupported(supportedUri, out _))
            throw new ArgumentException(
                $"URI '{supportedUri.Host}' not supported by {nameof(GooglePhotosResolver)}. " +
                "Supported: photos.app.goo.gl, photos.google.com, googleusercontent.com",
                nameof(supportedUri));

        logger.Debug("Resolving Google Photos URI: {Uri}", supportedUri);

        var html = await httpClient.GetStringAsync(supportedUri, cancellationToken).ConfigureAwait(false);
        var match = GooglePhotosUrlPattern.Match(html);

        if (!match.Success)
        {
            logger.Warning("Could not find direct image URL in Google Photos HTML for: {Uri}", supportedUri);
            return supportedUri;
        }

        var directUrl = match.Value;
        if (!directUrl.Contains('=') && !directUrl.EndsWith("=d"))
            directUrl += "=d"; // for raw quality

        var resolvedUri = new Uri(directUrl);
        logger.Debug("Resolved Google Photos URI to direct link: {ResolvedUri}", resolvedUri);
        return resolvedUri;
    }

    /// <summary>
    ///     Generates the regular expression used to find the direct image URL in Google Photos HTML.
    /// </summary>
    /// <returns>A compiled <see cref="Regex" /> instance.</returns>
    [GeneratedRegex(@"https://lh3\.googleusercontent\.com/pw/[^""\s?]+", RegexOptions.Compiled)]
    private static partial Regex GooglePhotosUrlRegex();
}


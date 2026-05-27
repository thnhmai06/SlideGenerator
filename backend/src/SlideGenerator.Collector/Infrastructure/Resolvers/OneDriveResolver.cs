/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector
 * File: OneDriveResolver.cs
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
using System.Text;
using SlideGenerator.Collector.Application.Abstractions;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Collector.Infrastructure.Resolvers;

/// <summary>
///     Provides access to Microsoft OneDrive as a cloud provider, converting sharing links to direct API download links.
/// </summary>
internal sealed class OneDriveResolver(ISystemLogger logger) : ICloudResolver
{
    /// <inheritdoc />
    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        key = CloudResolverKey.OneDrive;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host;
        return host.EndsWith("1drv.ms", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("onedrive.live.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (!IsUriSupported(supportedUri, out _))
            throw new ArgumentException(
                $"URI '{supportedUri.Host}' not supported by {nameof(OneDriveResolver)}. " +
                "Supported: 1drv.ms, onedrive.live.com",
                nameof(supportedUri));

        logger.Debug("Resolving OneDrive URI: {Uri}", supportedUri);

        var url = supportedUri.AbsoluteUri;
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');

        var resolvedUri = new Uri($"https://api.onedrive.com/v1.0/shares/{encodedUrl}/root/content");
        logger.Debug("Resolved OneDrive URI to direct link: {ResolvedUri}", resolvedUri);

        return Task.FromResult(resolvedUri);
    }
}

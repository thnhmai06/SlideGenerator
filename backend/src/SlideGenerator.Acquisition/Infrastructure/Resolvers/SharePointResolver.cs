/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition
 * File: SharePointResolver.cs
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

using System.Web;
using SlideGenerator.Acquisition.Application.Abstractions;
using SlideGenerator.Acquisition.Domain.Models;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Acquisition.Infrastructure.Resolvers;

/// <summary>
///     Provides access to Microsoft SharePoint as a cloud provider, resolving file URIs to direct download links.
/// </summary>
internal sealed class SharePointResolver(ISystemLogger logger) : ICloudResolver
{
    /// <inheritdoc />
    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        key = CloudResolverKey.SharePoint;
        return uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<Uri> ResolveUriAsync(Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (!IsUriSupported(supportedUri, out _))
            throw new ArgumentException(
                $"URI '{supportedUri.Host}' not supported by {nameof(SharePointResolver)}. " +
                "Supported: *.sharepoint.com",
                nameof(supportedUri));

        logger.Debug("Resolving SharePoint URI: {Uri}", supportedUri);

        if (string.IsNullOrEmpty(supportedUri.Query))
        {
            logger.Debug("SharePoint URI has no query parameters, returning as-is: {Uri}", supportedUri);
            return Task.FromResult(supportedUri);
        }

        var queryParams = HttpUtility.ParseQueryString(supportedUri.Query);
        var fileIdPath = queryParams.Get("id");

        if (!string.IsNullOrEmpty(fileIdPath) && fileIdPath.StartsWith('/'))
        {
            var fullHost = supportedUri.GetLeftPart(UriPartial.Authority);
            var resolvedUri = new Uri($"{fullHost}{fileIdPath}?download=1");
            logger.Debug("Resolved SharePoint URI to direct link: {ResolvedUri}", resolvedUri);
            return Task.FromResult(resolvedUri);
        }

        logger.Debug("SharePoint URI did not match expected 'id' parameter pattern, returning as-is: {Uri}",
            supportedUri);
        return Task.FromResult(supportedUri);
    }
}
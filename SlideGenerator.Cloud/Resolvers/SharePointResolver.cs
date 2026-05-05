/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
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
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Provides access to Microsoft SharePoint as a cloud provider, resolving file URIs to direct download links.
/// </summary>
internal sealed class SharePointResolver(ILogger logger) : CloudResolver(logger)
{
    /// <inheritdoc />
    public override Task<Uri> ResolveUriAsync(Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Resolving SharePoint URI: {Uri}", supportedUri);

        if (string.IsNullOrEmpty(supportedUri.Query))
        {
            Logger.LogDebug("SharePoint URI has no query parameters, returning as-is: {Uri}", supportedUri);
            return Task.FromResult(supportedUri);
        }

        var queryParams = HttpUtility.ParseQueryString(supportedUri.Query);
        var fileIdPath = queryParams.Get("id");

        if (!string.IsNullOrEmpty(fileIdPath) && fileIdPath.StartsWith('/'))
        {
            var fullHost = supportedUri.GetLeftPart(UriPartial.Authority);
            var resolvedUri = new Uri($"{fullHost}{fileIdPath}?download=1");
            Logger.LogDebug("Resolved SharePoint URI to direct link: {ResolvedUri}", resolvedUri);
            return Task.FromResult(resolvedUri);
        }

        Logger.LogDebug("SharePoint URI did not match expected 'id' parameter pattern, returning as-is: {Uri}",
            supportedUri);
        return Task.FromResult(supportedUri);
    }

    /// <inheritdoc />
    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase);
    }
}
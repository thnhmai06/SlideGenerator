/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
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
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Provides access to Microsoft OneDrive as a cloud provider, converting sharing links to direct API download links.
/// </summary>
internal sealed class OneDriveResolver(ILogger logger) : CloudResolver(logger)
{
    /// <inheritdoc />
    public override Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Resolving OneDrive URI: {Uri}", supportedUri);

        var url = supportedUri.AbsoluteUri;
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');

        var resolvedUri = new Uri($"https://api.onedrive.com/v1.0/shares/{encodedUrl}/root/content");
        Logger.LogDebug("Resolved OneDrive URI to direct link: {ResolvedUri}", resolvedUri);

        return Task.FromResult(resolvedUri);
    }

    /// <inheritdoc />
    public override bool IsUriSupported(Uri uri)
    {
        var host = uri.Host;
        return host.EndsWith("1drv.ms", StringComparison.OrdinalIgnoreCase)
               || host.EndsWith("onedrive.live.com", StringComparison.OrdinalIgnoreCase);
    }
}
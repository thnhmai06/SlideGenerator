/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: MultiCloudResolver.cs
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

using Microsoft.Extensions.Logging;
using SlideGenerator.Cloud.Models;
using SlideGenerator.Cloud.Resolvers;
using GoogleDriveResolver = SlideGenerator.Cloud.Resolvers.GoogleDriveResolver;
using GooglePhotosResolver = SlideGenerator.Cloud.Resolvers.GooglePhotosResolver;

namespace SlideGenerator.Cloud.Services;

public sealed class MultiCloudResolver(ILoggerFactory loggerFactory, ILogger<MultiCloudResolver> logger)
{
    private readonly IReadOnlyDictionary<CloudResolverKey, CloudResolver> _resolvers =
        new Dictionary<CloudResolverKey, CloudResolver>
        {
            {
                CloudResolverKey.GoogleDrive, new GoogleDriveResolver(loggerFactory.CreateLogger<GoogleDriveResolver>())
            },
            {
                CloudResolverKey.GooglePhotos,
                new GooglePhotosResolver(loggerFactory.CreateLogger<GooglePhotosResolver>())
            },
            { CloudResolverKey.OneDrive, new OneDriveResolver(loggerFactory.CreateLogger<OneDriveResolver>()) },
            { CloudResolverKey.SharePoint, new SharePointResolver(loggerFactory.CreateLogger<SharePointResolver>()) }
        }.AsReadOnly();

    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        foreach (var kvp in _resolvers)
            if (kvp.Value.IsUriSupported(uri))
            {
                key = kvp.Key;
                return true;
            }

        key = default;
        return false;
    }

    public async Task<Uri> ResolveUriAsync(
        Uri uri, HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (IsUriSupported(uri, out var key))
        {
            logger.LogDebug("URI {Uri} recognized as {CloudKey}. Delegating to specific resolver.", uri, key);
            return await _resolvers[key].ResolveUriAsync(uri, httpClient, cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogDebug("URI {Uri} is not recognized as a supported cloud provider. Returning as-is.", uri);
        return uri;
    }
}
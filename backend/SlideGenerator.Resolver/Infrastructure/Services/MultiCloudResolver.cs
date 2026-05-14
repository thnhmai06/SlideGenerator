/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Resolver
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

using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Cloud.Domain.Models;
using SlideGenerator.Cloud.Infrastructure.Resolvers;
using SlideGenerator.Logging.Domain.Abstractions;
using GoogleDriveResolver = SlideGenerator.Cloud.Infrastructure.Resolvers.GoogleDriveResolver;
using GooglePhotosResolver = SlideGenerator.Cloud.Infrastructure.Resolvers.GooglePhotosResolver;

namespace SlideGenerator.Cloud.Infrastructure.Services;

internal sealed class MultiCloudResolver(ISystemLogger logger) : ICloudResolver
{
    private readonly IReadOnlyDictionary<CloudResolverKey, ICloudResolver> _resolvers =
        new Dictionary<CloudResolverKey, ICloudResolver>
        {
            { CloudResolverKey.GoogleDrive, new GoogleDriveResolver(logger) },
            { CloudResolverKey.GooglePhotos, new GooglePhotosResolver(logger) },
            { CloudResolverKey.OneDrive, new OneDriveResolver(logger) },
            { CloudResolverKey.SharePoint, new SharePointResolver(logger) }
        }.AsReadOnly();

    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        foreach (var kvp in _resolvers)
            if (kvp.Value.IsUriSupported(uri, out key))
                return true;

        key = default;
        return false;
    }

    public async Task<Uri> ResolveUriAsync(
        Uri uri, HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (IsUriSupported(uri, out var key))
        {
            logger.Debug("URI {Uri} recognized as {CloudKey}. Delegating to specific resolver.", uri, key);
            return await _resolvers[key].ResolveUriAsync(uri, httpClient, cancellationToken)
                .ConfigureAwait(false);
        }

        logger.Debug("URI {Uri} is not recognized as a supported cloud provider. Returning as-is.", uri);
        return uri;
    }
}
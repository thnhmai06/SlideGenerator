/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector
 * File: GoogleDriveResolver.cs
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
using System.Web;
using SlideGenerator.Collector.Application.Abstractions;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Logging.Domain.Abstractions;

namespace SlideGenerator.Collector.Infrastructure.Resolvers;

internal sealed partial class GoogleDriveResolver(ISystemLogger logger) : ICloudResolver
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();

    public bool IsUriSupported(Uri uri, out CloudResolverKey key)
    {
        key = CloudResolverKey.GoogleDrive;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        return uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (!IsUriSupported(supportedUri, out _))
            throw new ArgumentException(
                $"URI '{supportedUri.Host}' not supported by {nameof(GoogleDriveResolver)}. Supported: drive.google.com",
                nameof(supportedUri));

        logger.Debug("Resolving Google Drive URI: {Uri}", supportedUri);

        string? fileId = null;
        var url = supportedUri.AbsoluteUri;

        if (supportedUri.AbsolutePath.Contains("/file/d/"))
        {
            var match = GoogleDriveFileIdPattern.Match(url);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }
        else if (supportedUri.Query.Contains("id="))
        {
            var query = HttpUtility.ParseQueryString(supportedUri.Query);
            fileId = query["id"];
        }
        else if (supportedUri.AbsolutePath.Contains("/folders/"))
        {
            logger.Debug("Resolving Google Drive folder URI by fetching HTML content");
            var html = await httpClient.GetStringAsync(supportedUri, cancellationToken).ConfigureAwait(false);
            var match = GoogleDriveFileIdPattern.Match(html);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }

        if (string.IsNullOrEmpty(fileId))
        {
            logger.Warning("Could not extract File ID from Google Drive URI: {Uri}", supportedUri);
            return supportedUri;
        }

        var resolvedUri = new Uri($"https://drive.google.com/uc?export=download&id={fileId}");
        logger.Debug("Resolved Google Drive URI to direct link: {ResolvedUri}", resolvedUri);
        return resolvedUri;
    }

    [GeneratedRegex(@"/file/d/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();
}

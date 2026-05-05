/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
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
using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

internal sealed partial class GoogleDriveResolver(ILogger logger) : CloudResolver(logger)
{
    private static readonly Regex GoogleDriveFileIdPattern = GoogleDriveFileIdRegex();

    public override async Task<Uri> ResolveUriAsync(
        Uri supportedUri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Resolving Google Drive URI: {Uri}", supportedUri);

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
            Logger.LogDebug("Resolving Google Drive folder URI by fetching HTML content");
            var html = await httpClient.GetStringAsync(supportedUri, cancellationToken).ConfigureAwait(false);
            var match = GoogleDriveFileIdPattern.Match(html);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }

        if (string.IsNullOrEmpty(fileId))
        {
            Logger.LogWarning("Could not extract File ID from Google Drive URI: {Uri}", supportedUri);
            return supportedUri;
        }

        var resolvedUri = new Uri($"https://drive.google.com/uc?export=download&id={fileId}");
        Logger.LogDebug("Resolved Google Drive URI to direct link: {ResolvedUri}", resolvedUri);
        return resolvedUri;
    }

    public override bool IsUriSupported(Uri uri)
    {
        return uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"/file/d/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex GoogleDriveFileIdRegex();
}
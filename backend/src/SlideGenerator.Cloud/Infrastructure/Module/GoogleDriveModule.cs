/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: GoogleDriveModule.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text.RegularExpressions;
using System.Web;
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Cloud.Domain.Models;

namespace SlideGenerator.Cloud.Infrastructure.Module;

/// <summary>
///     Resolves Google Drive sharing links to direct download URIs.
///     Supports file links (<c>/file/d/…</c>), <c>uc?id=…</c> style links, and folder links.
///     For folder links, returns the download URI of the first direct-child file found;
///     returns <see langword="null" /> when the folder is empty, contains only subfolders,
///     or is inaccessible.
/// </summary>
internal sealed partial class GoogleDriveModule : CloudResolveModule
{
    private const string EmbeddedFolderViewBase = "https://drive.google.com/embeddedfolderview?id=";
    private const string DownloadBase = "https://drive.google.com/uc?export=download&id=";

    /// <inheritdoc />
    /// <remarks>
    ///     Matches any URI whose host ends with <c>drive.google.com</c> (case-insensitive).
    /// </remarks>
    public override bool IsResolvable(Uri uri, out CloudHost key)
    {
        key = CloudHost.GoogleDrive;
        return uri.Host.EndsWith("drive.google.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Resolution strategy:
    ///     <list type="bullet">
    ///         <item>
    ///             <c>/file/d/{id}</c> — extracts file ID directly from the path and returns the
    ///             corresponding <c>uc?export=download</c> URI.
    ///         </item>
    ///         <item>
    ///             <c>?id={id}</c> query parameter — treated as a direct file reference.
    ///         </item>
    ///         <item>
    ///             <c>/folders/{id}</c> — fetches the embedded folder view
    ///             (<c>embeddedfolderview?id=…</c>) and scans the HTML for the first
    ///             <c>/file/d/</c> link. Returns <see langword="null" /> when none is found or
    ///             when the request fails (e.g., HTTP 4xx/5xx, network error).
    ///         </item>
    ///     </list>
    ///     Returns <see langword="null" /> when no file ID can be determined.
    /// </remarks>
    public override async Task<Uri?> ResolveAsync(
        Uri uri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (!IsResolvable(uri, out _))
            throw new ArgumentException(
                $"URI '{uri}' is not supported by {nameof(GoogleDriveModule)}.", nameof(uri));

        string? fileId = null;

        if (uri.AbsolutePath.Contains("/file/d/"))
        {
            var match = FileIdInPathRegex().Match(uri.AbsoluteUri);
            if (match.Success)
                fileId = match.Groups[1].Value;
        }
        else if (uri.Query.Contains("id="))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            fileId = query["id"];
        }
        else if (uri.AbsolutePath.Contains("/folders/"))
        {
            var folderMatch = FolderIdInPathRegex().Match(uri.AbsolutePath);
            if (folderMatch.Success)
                fileId = await GetFirstFileIdFromFolderAsync(
                    folderMatch.Groups[1].Value, httpClient, cancellationToken).ConfigureAwait(false);
        }

        return string.IsNullOrEmpty(fileId) ? null : new Uri(DownloadBase + fileId);
    }

    #region Private helpers

    /// <summary>
    ///     Fetches the embedded folder view for <paramref name="folderId" /> and returns the ID of
    ///     the first direct-child file found in the HTML, or <see langword="null" /> when no file
    ///     link is present or the request fails.
    /// </summary>
    private static async Task<string?> GetFirstFileIdFromFolderAsync(
        string folderId,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var html = await httpClient
                .GetStringAsync(EmbeddedFolderViewBase + folderId, cancellationToken)
                .ConfigureAwait(false);
            var match = FileIdInHtmlRegex().Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Regex

    /// <summary>Extracts a file ID from a <c>/file/d/{id}</c> URL segment.</summary>
    [GeneratedRegex(@"/file/d/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex FileIdInPathRegex();

    /// <summary>Extracts a folder ID from a <c>/folders/{id}</c> URL segment.</summary>
    [GeneratedRegex(@"/folders/([^/?]+)", RegexOptions.Compiled)]
    private static partial Regex FolderIdInPathRegex();

    /// <summary>Extracts a file ID from a <c>/file/d/{id}</c> link found in the HTML source.</summary>
    [GeneratedRegex(@"/file/d/([^""'/?]+)", RegexOptions.Compiled)]
    private static partial Regex FileIdInHtmlRegex();

    #endregion
}
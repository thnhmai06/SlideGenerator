/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ImageSourcePolicy.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     What a worksheet cell value is allowed to mean as an image source (see <see cref="ImageSourcePolicy" />).
/// </summary>
internal enum ImageSourceKind
{
    /// <summary>The value is not a usable image source and must never reach the network or the file system.</summary>
    Ignored,

    /// <summary>The value names an existing local file, accepted only because the request allows local paths.</summary>
    LocalFile,

    /// <summary>The value is an absolute <c>http</c>/<c>https</c> URL that may be inspected and downloaded.</summary>
    Url
}

/// <summary>
///     Decides what an image-source cell value may mean, so a data file cannot make the application read
///     arbitrary local images when the request forbids it, and free text such as <c>John Smith</c> is never
///     turned into a network request. A value is accepted as a URL only when, after prepending
///     <c>https://</c> to a scheme-less value, it parses as an absolute <c>http</c>/<c>https</c> URI whose host
///     is a dotted DNS name or an IP address — a single-label host (<c>abc</c>, <c>localhost</c>), a UNC path,
///     <c>file:///...</c> and <c>ftp://...</c> are all ignored.
/// </summary>
/// <remarks>
///     A recipe's own <c>FallbackImagePath</c> is <em>not</em> subject to this policy: it belongs to the recipe
///     author and is always allowed.
/// </remarks>
internal static class ImageSourcePolicy
{
    /// <summary>
    ///     Classifies <paramref name="source" /> in order: blank values are <see cref="ImageSourceKind.Ignored" />;
    ///     an existing local file is <see cref="ImageSourceKind.LocalFile" /> only when
    ///     <paramref name="allowLocalPaths" /> is <see langword="true" /> (otherwise it falls through and is
    ///     ignored); a network URL is <see cref="ImageSourceKind.Url" /> with the parsed <paramref name="url" />;
    ///     everything else is <see cref="ImageSourceKind.Ignored" />.
    /// </summary>
    /// <param name="source">The raw cell value.</param>
    /// <param name="allowLocalPaths">Whether the request permits cell values that name existing local files.</param>
    /// <param name="url">The parsed URL when the result is <see cref="ImageSourceKind.Url" />; otherwise <see langword="null" />.</param>
    /// <returns>What the value is allowed to mean.</returns>
    public static ImageSourceKind Classify(string? source, bool allowLocalPaths, out Uri? url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(source)) return ImageSourceKind.Ignored;

        if (allowLocalPaths && File.Exists(source)) return ImageSourceKind.LocalFile;

        var value = source.Trim();
        if (!value.Contains("://", StringComparison.Ordinal)) value = "https://" + value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return ImageSourceKind.Ignored;
        if (uri.Scheme is not ("http" or "https")) return ImageSourceKind.Ignored;

        var hostType = Uri.CheckHostName(uri.Host);
        if (hostType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
            return ImageSourceKind.Ignored;
        if (hostType == UriHostNameType.Dns && !uri.Host.Contains('.', StringComparison.Ordinal))
            return ImageSourceKind.Ignored;

        url = uri;
        return ImageSourceKind.Url;
    }
}

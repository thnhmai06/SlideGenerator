/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: Utilities.cs
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

using System.Diagnostics.CodeAnalysis;

namespace SlideGenerator.Cloud.Application;

/// <summary>
///     Shared URI parsing utilities used across the Cloud module.
/// </summary>
internal static class Utilities
{
    /// <summary>
    ///     Tries to parse <paramref name="url" /> as an absolute <see cref="Uri" />.
    ///     Prepends <c>https://</c> when <paramref name="url" /> contains no scheme separator (<c>://</c>).
    /// </summary>
    /// <returns>
    ///     A valid absolute <see cref="Uri" />, or <see langword="null" /> when
    ///     <paramref name="url" /> is empty, whitespace, or cannot be parsed.
    /// </returns>
    public static bool TryCreateUri(string? url, [MaybeNullWhen(false)] out Uri uri)
    {
        uri = null;

        url = url?.Trim();
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!url.Contains("://")) url = Uri.UriSchemeHttps + "://" + url;
        return Uri.TryCreate(url, UriKind.Absolute, out uri);
    }

    /// <summary>
    ///     Tries to parse <paramref name="url" /> as an absolute <see cref="Uri" />.
    ///     Returns <see langword="null" /> when parsing fails.
    /// </summary>
    public static Uri? TryCreateUri(string? url)
    {
        return TryCreateUri(url, out var uri) ? uri : null;
    }
}
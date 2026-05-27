/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: ContentInfo.cs
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

namespace SlideGenerator.Cloud.Domain.Models;

/// <summary>
///     Holds metadata about a remote resource obtained by inspecting its HTTP response headers.
/// </summary>
/// <param name="Uri">Final URI of the resource after following all HTTP redirects.</param>
/// <param name="Type">
///     MIME content-type (e.g. <c>image/jpeg</c>), or <see langword="null" /> when the server did not
///     supply one.
/// </param>
/// <param name="Length">Content length in bytes, or <see langword="null" /> when unknown.</param>
public record ContentInfo(Uri Uri, string? Type, uint? Length)
{
    /// <summary>
    ///     Returns <see langword="true" /> when <see cref="Type" /> starts with <c>image/</c>
    ///     (case-insensitive), indicating the resource is an image.
    ///     Returns <see langword="false" /> when <see cref="Type" /> is <see langword="null" />.
    /// </summary>
    public bool IsImage()
    {
        return Type?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
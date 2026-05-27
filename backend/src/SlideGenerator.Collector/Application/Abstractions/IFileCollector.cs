/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector
 * File: IFileCollector.cs
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
using SlideGenerator.Collector.Domain.Models;

namespace SlideGenerator.Collector.Application.Abstractions;

/// <summary>
///     Acquires an image from a direct URL or local file path to a local destination.
///     Cloud sharing links must be resolved to direct URLs before calling this service.
///     Whether a local path is permitted is determined by the caller before invoking.
/// </summary>
public interface IFileCollector
{
    /// <summary>
    ///     Returns whether <paramref name="source" /> points to a valid image, together with its resolved location.
    ///     For local files: tries to open the file as an image; resolved location is the original path.
    ///     For URLs: sends an HTTP HEAD request, checks <c>Content-Type</c> starts with <c>image/</c>,
    ///     and returns the final URI after redirects as the resolved location.
    /// </summary>
    /// <param name="source">Direct URL or local file path to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     A tuple of (<c>IsValid</c>, <c>ResolvedSource</c>).
    ///     <c>ResolvedSource</c> is the final path or URL after resolution; <see langword="null" /> only when
    ///     the source cannot be parsed as a URI and no file exists at that path.
    /// </returns>
    Task<(bool IsValid, string ResolvedSource)> IsImageSourceAsync(string source, CancellationToken ct = default);

    /// <summary>
    ///     Downloads or copies an image to <paramref name="savePath" />.
    ///     Validates the source via <see cref="IsImageSourceAsync" /> before proceeding;
    ///     throws <see cref="ArgumentException" /> if the source is not a valid image.
    ///     Whether a local path is allowed must be checked by the caller before invoking this method.
    /// </summary>
    /// <param name="source">Direct HTTP(S) URL or local file path. Cloud sharing links must be pre-resolved.</param>
    /// <param name="savePath">Local path to write the image to.</param>
    /// <param name="configuration">Download settings (retry, timeout, proxy) supplied per-call by the caller.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="source" /> is not a valid image source.
    /// </exception>
    Task AcquireImageAsync(
        string source,
        string savePath,
        DownloadConfiguration configuration,
        CancellationToken ct = default);
}


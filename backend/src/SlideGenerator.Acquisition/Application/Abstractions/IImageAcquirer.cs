/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition
 * File: IImageAcquirer.cs
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

using SlideGenerator.Acquisition.Domain.Models;

namespace SlideGenerator.Acquisition.Application.Abstractions;

/// <summary>
///     Acquires an image from a URL or local file path to a local destination.
/// </summary>
public interface IImageAcquirer
{
    /// <summary>
    ///     Downloads or copies an image to <paramref name="savePath" />.
    /// </summary>
    /// <param name="urlOrPath">
    ///     Raw URL string — a cloud sharing link, a plain HTTP(S) URL, or a local file path
    ///     when <paramref name="allowLocalPath" /> is <see langword="true" />.
    /// </param>
    /// <param name="savePath">Local path to write the image to.</param>
    /// <param name="configuration">Download settings (retry, timeout, proxy) supplied per-call by the caller.</param>
    /// <param name="allowLocalPath">
    ///     When <see langword="true" /> and <paramref name="urlOrPath" /> matches an existing file,
    ///     creates a hard link; falls back to <see cref="File.Copy(string,string,bool)" /> if the link fails.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task AcquireAsync(
        string urlOrPath,
        string savePath,
        DownloadConfiguration configuration,
        bool allowLocalPath = false,
        CancellationToken ct = default);
}
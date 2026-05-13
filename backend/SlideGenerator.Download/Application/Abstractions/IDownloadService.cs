/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Download
 * File: IDownloadService.cs
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
namespace SlideGenerator.Download.Application.Abstractions;

/// <summary>
///     Defines the contract for downloading remote resources to local storage.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    ///     Downloads the resource at <paramref name="uri" /> to <paramref name="destinationPath" />.
    /// </summary>
    /// <param name="uri">The remote resource URI.</param>
    /// <param name="destinationPath">The local path to write the downloaded content to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DownloadAsync(Uri uri, string destinationPath, CancellationToken ct = default);
}







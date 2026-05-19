/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition
 * File: ICloudResolver.cs
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
///     Resolves cloud provider sharing links to direct download URIs.
/// </summary>
public interface ICloudResolver
{
    /// <summary>
    ///     Returns <see langword="true" /> when this resolver can handle <paramref name="uri" />,
    ///     and sets <paramref name="key" /> to the corresponding provider.
    /// </summary>
    bool IsUriSupported(Uri uri, out CloudResolverKey key);

    /// <summary>
    ///     Resolves <paramref name="supportedUri" /> to a direct download URI.
    ///     Callers must verify support via <see cref="IsUriSupported" /> before calling this method.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="supportedUri" /> is not supported by this resolver.
    /// </exception>
    Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient,
        CancellationToken cancellationToken = default);
}
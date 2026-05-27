/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: CloudResolveModule.cs
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

using SlideGenerator.Cloud.Domain.Models;

namespace SlideGenerator.Cloud.Application.Abstractions;

/// <summary>
///     Defines the contract for a single provider resolver module.
///     Modules convert provider-specific sharing links into direct download URIs.
///     Use <see cref="ICloudResolver" /> for the public-facing composite resolver.
/// </summary>
internal abstract class CloudResolveModule
{
    /// <summary>
    ///     Returns <see langword="true" /> when this module can handle <paramref name="uri" />,
    ///     and sets <paramref name="key" /> to the corresponding provider.
    /// </summary>
    public abstract bool IsResolvable(Uri uri, out CloudHost key);

    /// <summary>
    ///     Resolves <paramref name="uri" /> to a direct download URI, or returns
    ///     <see langword="null" /> when no downloadable resource can be found
    ///     (e.g., permission denied, empty folder, non-existent file).
    ///     Callers must verify support via <see cref="IsResolvable" /> before calling this method.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="uri" /> is not supported by this module.
    /// </exception>
    public abstract Task<Uri?> ResolveAsync(
        Uri uri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default);
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: CloudResolver.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Cloud.Application.Abstractions;

/// <summary>
///     Defines the contract for a single provider resolver module.
///     Modules convert provider-specific sharing links into direct download URIs.
///     Use <see cref="ICloudClient" /> for the public-facing composite client.
/// </summary>
internal abstract class CloudResolver
{
    /// <summary>
    ///     Returns <see langword="true" /> when this module can handle <paramref name="uri" />.
    /// </summary>
    public abstract bool IsResolvable(Uri uri);

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
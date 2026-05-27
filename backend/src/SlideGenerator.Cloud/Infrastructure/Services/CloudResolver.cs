/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud
 * File: CloudResolver.cs
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

using System.Collections.ObjectModel;
using SlideGenerator.Cloud.Application;
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Cloud.Domain.Models;
using SlideGenerator.Cloud.Infrastructure.Module;

namespace SlideGenerator.Cloud.Infrastructure.Services;

/// <summary>
///     Composite resolver that routes cloud sharing links to the appropriate provider module,
///     first expanding any HTTP redirects (e.g., short links) before provider matching.
///     Registered modules: Google Drive.
/// </summary>
internal sealed class CloudResolver(ICloudClient cloudClient) : ICloudResolver
{
    private readonly ReadOnlyDictionary<CloudHost, CloudResolveModule> _resolvers =
        new Dictionary<CloudHost, CloudResolveModule>
        {
            { CloudHost.GoogleDrive, new GoogleDriveModule() }
        }.AsReadOnly();

    /// <inheritdoc />
    public bool GetCloudHost(string url, out CloudHost key)
    {
        if (Utilities.TryCreateUri(url, out var uri))
            return CanBeResolved(uri, out key);

        key = default;
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Execution flow:
    ///     <list type="number">
    ///         <item>
    ///             Parse <paramref name="url" /> into a <see cref="Uri" /> (auto-injects https:// when the scheme is
    ///             absent).
    ///         </item>
    ///         <item>Reject non-HTTP/HTTPS schemes with <see cref="ArgumentException" />.</item>
    ///         <item>
    ///             Inspect the URI via <see cref="ICloudClient.InspectAsync" /> to follow redirects and resolve the final
    ///             URI.
    ///         </item>
    ///         <item>Route to the matching provider module; if none matches, return the final URI unchanged.</item>
    ///         <item>Return the module's result, which may be <see langword="null" /> for inaccessible resources.</item>
    ///     </list>
    /// </remarks>
    public async Task<Uri?> ResolveAsync(
        string url,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        if (!Utilities.TryCreateUri(url, out var uri)) return null;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException(
                $"Unsupported URI scheme '{uri.Scheme}'. Only HTTP and HTTPS are supported.",
                nameof(url));

        httpClient ??= new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });

        var info = await cloudClient.InspectAsync(uri, httpClient, cancellationToken).ConfigureAwait(false);
        uri = info?.Uri ?? uri;

        if (!CanBeResolved(uri, out var host)) return uri;

        return await _resolvers[host]
            .ResolveAsync(uri, httpClient, cancellationToken)
            .ConfigureAwait(false);
    }

    #region Private helpers

    /// <summary>
    ///     Iterates registered modules and returns <see langword="true" /> when one reports it can
    ///     resolve <paramref name="uri" />, setting <paramref name="host" /> to that provider.
    /// </summary>
    private bool CanBeResolved(Uri uri, out CloudHost host)
    {
        foreach (var kvp in _resolvers)
            if (kvp.Value.IsResolvable(uri, out host))
                return true;

        host = default;
        return false;
    }

    #endregion
}
/*
 * Copyright (C) 2026 Thành Mai
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

using Microsoft.Extensions.Logging;

namespace SlideGenerator.Cloud.Resolvers;

/// <summary>
///     Defines a contract for resolving cloud-hosted URIs to direct download links.
/// </summary>
public abstract class CloudResolver(ILogger logger)
{
    protected ILogger Logger { get; } = logger;

    public abstract bool IsUriSupported(Uri uri);

    public abstract Task<Uri> ResolveUriAsync(Uri supportedUri, HttpClient httpClient,
        CancellationToken cancellationToken = default);
}
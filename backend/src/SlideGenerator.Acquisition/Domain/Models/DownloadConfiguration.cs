/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition
 * File: DownloadConfiguration.cs
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

using System.Net;

namespace SlideGenerator.Acquisition.Domain.Models;

/// <summary>
///     Configures HTTP download behavior. Passed per-call so callers control retry/timeout/proxy
///     without requiring the Acquisition module to depend on the Settings module.
/// </summary>
public sealed record DownloadConfiguration
{
    /// <summary>Gets the maximum number of retry attempts on transient failure.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Gets the per-block timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Gets the optional HTTP proxy to use.</summary>
    public IWebProxy? Proxy { get; init; }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.NetworkSetting.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Net;

namespace SlideGenerator.Settings.Domain.Entities;

public sealed partial record Setting
{
    public sealed record NetworkSetting
    {
        public Proxy Proxy { get; init; } = new();

        /// <summary>Gets the settings for retry logic and timeouts.</summary>
        public RetrySetting Retry { get; init; } = new();
    }

    /// <summary>
    ///     Provides network proxy details for corporate or restricted environments.
    /// </summary>
    public sealed record Proxy
    {
        /// <summary>Gets whether a proxy should be used.</summary>
        public bool UseProxy { get; init; } = false;

        /// <summary>Gets the proxy domain name.</summary>
        public string Domain { get; init; } = string.Empty;

        /// <summary>Gets the proxy password.</summary>
        public string Password { get; init; } = string.Empty;

        /// <summary>Gets the full proxy server address (e.g., http://proxy:8080).</summary>
        public string ProxyAddress { get; init; } = string.Empty;

        /// <summary>Gets the proxy username.</summary>
        public string Username { get; init; } = string.Empty;

        /// <summary>
        ///     Constructs an <see cref="IWebProxy" /> based on the current configuration.
        /// </summary>
        /// <returns>A configured web proxy, or null if proxy usage is disabled.</returns>
        public IWebProxy? GetWebProxy()
        {
            if (!UseProxy || string.IsNullOrEmpty(ProxyAddress))
                return null;

            var proxy = new WebProxy(ProxyAddress)
            {
                Credentials = new NetworkCredential(Username, Password, Domain)
            };
            return proxy;
        }
    }

    /// <summary>
    ///     Configures the behavior of network request retries.
    /// </summary>
    public sealed record RetrySetting
    {
        /// <summary>Gets the maximum number of times a failed request should be retried.</summary>
        public int MaxRetries { get; init; } = 3;

        /// <summary>Gets the network timeout in seconds.</summary>
        public int Timeout { get; init; } = 30;
    }
}
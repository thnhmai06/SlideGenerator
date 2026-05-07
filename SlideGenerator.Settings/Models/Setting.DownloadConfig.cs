/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.DownloadConfig.cs
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
using SlideGenerator.Hash.Services;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Settings.Models;

public sealed partial record Setting
{
    /// <summary>
    ///     Settings governing how the application downloads resources and handles network connectivity.
    /// </summary>
    public sealed record DownloadSetting
    {
        /// <summary>Gets the settings for temporary file storage.</summary>
        public TempSetting Temp { get; init; } = new();

        /// <summary>Gets the settings for network proxy configuration.</summary>
        public ProxySetting Proxy { get; init; } = new();

        /// <summary>Gets the settings for retry logic and timeouts.</summary>
        public RetrySetting Retry { get; init; } = new();

        /// <summary>
        ///     Defines where temporary files are stored and how directory paths are structured.
        /// </summary>
        public sealed record TempSetting
        {
            /// <summary>
            ///     Gets or sets the base directory for temporary application files.
            ///     Defaults to the system temporary path if not specified.
            /// </summary>
            public string FolderPath
            {
                get => string.IsNullOrEmpty(field) ? NameAndPathRules.DefaultTempPath : field;
                set => field = value;
            } = string.Empty;

            /// <summary>
            ///     Constructs a specialized directory path for storing raw downloaded images.
            /// </summary>
            /// <param name="bookPath">The absolute path to the source workbook.</param>
            /// <param name="sheetName">The name of the source worksheet.</param>
            /// <param name="colName">The name of the column providing the image URI.</param>
            /// <param name="registry">The path registry service.</param>
            /// <returns>A full directory path for downloads.</returns>
            public string GetDownloadDir(string bookPath, string sheetName, string colName, HashPathRegistry registry)
            {
                var bookName = Path.GetFileNameWithoutExtension(bookPath);
                var hash = registry.GetShortHash(bookPath);
                var bookFolder = $"{Utilities.NormalizeFileName(bookName)}_{hash}";
                sheetName = Utilities.NormalizeFileName(sheetName);
                colName = Utilities.NormalizeFileName(colName);
                return Path.Combine(FolderPath, bookFolder, sheetName, colName, "Download");
            }

            /// <summary>
            ///     Constructs a specialized directory path for storing edited (cropped/resized) images.
            /// </summary>
            /// <param name="bookPath">The absolute path to the source workbook.</param>
            /// <param name="sheetName">The name of the source worksheet.</param>
            /// <param name="colName">The name of the column providing the image URI.</param>
            /// <param name="registry">The path registry service.</param>
            /// <returns>A full directory path for edited images.</returns>
            public string GetEditDir(string bookPath, string sheetName, string colName, HashPathRegistry registry)
            {
                var bookName = Path.GetFileNameWithoutExtension(bookPath);
                var hash = registry.GetShortHash(bookPath);
                var bookFolder = $"{Utilities.NormalizeFileName(bookName)}_{hash}";
                sheetName = Utilities.NormalizeFileName(sheetName);
                colName = Utilities.NormalizeFileName(colName);
                return Path.Combine(FolderPath, bookFolder, sheetName, colName, "Edit");
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

        /// <summary>
        ///     Provides network proxy details for corporate or restricted environments.
        /// </summary>
        public sealed record ProxySetting
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
    }
}
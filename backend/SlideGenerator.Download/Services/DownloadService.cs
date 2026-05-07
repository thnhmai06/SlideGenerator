/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Download
 * File: DownloadService.cs
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

using Downloader;
using Microsoft.Extensions.Logging;
using SlideGenerator.Settings.Services;

namespace SlideGenerator.Download.Services;

public sealed class DownloadService(ISettingProvider settingProvider, ILogger<DownloadService> logger)
{
    public async Task DownloadAsync(Uri uri, string destinationPath, CancellationToken ct = default)
    {
        var setting = settingProvider.Current;
        var config = new DownloadConfiguration
        {
            BlockTimeout = setting.Download.Retry.Timeout * 1000,
            MaxTryAgainOnFailure = setting.Download.Retry.MaxRetries,
            RequestConfiguration = { Proxy = setting.Download.Proxy.GetWebProxy() }
        };

        logger.LogDebug("Initiating download from {Uri} to {Destination}", uri, destinationPath);

        try
        {
            await using var service = new Downloader.DownloadService(config);
            await service.DownloadFileTaskAsync(uri.ToString(), destinationPath, ct).ConfigureAwait(false);
            logger.LogInformation("Successfully downloaded file to {Destination}", destinationPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file from {Uri} to {Destination}", uri, destinationPath);
            throw;
        }
    }
}
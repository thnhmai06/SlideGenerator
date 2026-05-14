/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
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
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Settings.Application.Abstractions;
using IDownloadService = SlideGenerator.Download.Application.Abstractions.IDownloadService;

namespace SlideGenerator.Download.Infrastructure.Services;

internal sealed class DownloadService(ISettingProvider settingProvider, ISystemLogger logger)
    : IDownloadService
{
    public async Task DownloadAsync(Uri uri, string destinationPath, CancellationToken ct = default)
    {
        var setting = settingProvider.Current;
        var config = new DownloadConfiguration
        {
            BlockTimeout = setting.Network.Retry.Timeout * 1000,
            MaxTryAgainOnFailure = setting.Network.Retry.MaxRetries,
            RequestConfiguration = { Proxy = setting.Network.Proxy.GetWebProxy() }
        };

        logger.Debug("Initiating download from {Uri} to {Destination}", uri, destinationPath);

        try
        {
            await using var service = new Downloader.DownloadService(config);
            await service.DownloadFileTaskAsync(uri.ToString(), destinationPath, ct).ConfigureAwait(false);
            logger.Information("Successfully downloaded file to {Destination}", destinationPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to download file from {Uri} to {Destination}", uri, destinationPath);
            throw;
        }
    }
}
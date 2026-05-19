/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition
 * File: ImageAcquirer.cs
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

using SlideGenerator.Acquisition.Application.Abstractions;
using SlideGenerator.Acquisition.Domain.Models;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Utilities.Helper;
using DownloaderConfig = Downloader.DownloadConfiguration;
using DownloaderService = Downloader.DownloadService;
using HardLink = SlideGenerator.Utilities.Helper.HardLink;

namespace SlideGenerator.Acquisition.Infrastructure.Services;

internal sealed class ImageAcquirer(
    ICloudResolver cloudResolver,
    IHttpClientFactory httpClientFactory,
    ISystemLogger logger) : IImageAcquirer
{
    public async Task AcquireAsync(
        string urlOrPath,
        string savePath,
        DownloadConfiguration configuration,
        bool allowLocalPath = false,
        CancellationToken ct = default)
    {
        if (allowLocalPath && File.Exists(urlOrPath))
        {
            logger.Debug("Local file found at '{Url}'. Creating hard link to '{Dest}'", urlOrPath, savePath);
            try
            {
                HardLink.Create(savePath, urlOrPath);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Hard link failed for '{Url}'. Falling back to file copy.", urlOrPath);
                File.Copy(urlOrPath, savePath, true);
            }

            return;
        }

        var uri = Normalization.NormalizeUri(urlOrPath)
                  ?? throw new ArgumentException($"'{urlOrPath}' is not a valid URI.", nameof(urlOrPath));

        var httpClient = httpClientFactory.CreateClient();
        var resolvedUri = await cloudResolver.ResolveUriAsync(uri, httpClient, ct).ConfigureAwait(false);

        logger.Debug("Downloading from '{ResolvedUri}' to '{Dest}'", resolvedUri, savePath);

        var config = new DownloaderConfig
        {
            BlockTimeout = configuration.TimeoutSeconds * 1000,
            MaxTryAgainOnFailure = configuration.MaxRetries,
            RequestConfiguration = { Proxy = configuration.Proxy }
        };

        await using var service = new DownloaderService(config);
        await service.DownloadFileTaskAsync(resolvedUri.ToString(), savePath, ct).ConfigureAwait(false);

        logger.Debug("Downloaded successfully to '{Dest}'", savePath);
    }
}
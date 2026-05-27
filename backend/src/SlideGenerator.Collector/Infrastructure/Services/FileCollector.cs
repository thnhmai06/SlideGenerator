/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector
 * File: FileCollector.cs
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
using SlideGenerator.Collector.Application.Abstractions;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Utilities.Helper;
using DownloaderConfig = Downloader.DownloadConfiguration;
using DownloaderService = Downloader.DownloadService;
using HardLink = SlideGenerator.Utilities.Helper.HardLink;

namespace SlideGenerator.Collector.Infrastructure.Services;

internal sealed class FileCollector(
    IImageFactory imageFactory,
    IHttpClientFactory httpClientFactory,
    ISystemLogger logger) : IFileCollector
{
    #region IFileCollector

    /// <inheritdoc />
    public async Task<(bool IsValid, string ResolvedSource)> IsImageSourceAsync(
        string source,
        CancellationToken ct = default)
    {
        if (File.Exists(source))
            try
            {
                using var img = imageFactory.Open(source);
                return (true, source);
            }
            catch
            {
                return (false, source);
            }

        var uri = Normalization.NormalizeUri(source);
        if (uri is null) return (false, source);

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient
                .SendAsync(new HttpRequestMessage(HttpMethod.Head, uri), ct)
                .ConfigureAwait(false);
            var resolved = response.RequestMessage?.RequestUri?.ToString() ?? uri.ToString();
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var isImage = contentType is not null &&
                          contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            return (isImage, resolved);
        }
        catch
        {
            return (false, uri.ToString());
        }
    }

    /// <inheritdoc />
    public async Task AcquireImageAsync(
        string source,
        string savePath,
        DownloadConfiguration configuration,
        CancellationToken ct = default)
    {
        var (isValid, resolvedSource) = await IsImageSourceAsync(source, ct).ConfigureAwait(false);
        if (!isValid)
            throw new ArgumentException($"'{source}' is not a valid image source.", nameof(source));

        if (File.Exists(source))
        {
            await CollectFromLocalAsync(source, savePath, ct).ConfigureAwait(false);
            return;
        }

        var uri = new Uri(resolvedSource);
        await CollectFromUrlAsync(uri, savePath, configuration, ct).ConfigureAwait(false);
    }

    #endregion

    #region Private

    private Task CollectFromLocalAsync(string path, string savePath, CancellationToken ct)
    {
        logger.Debug("Local file found at '{Path}'. Creating hard link to '{Dest}'", path, savePath);
        try
        {
            HardLink.Create(savePath, path);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Hard link failed for '{Path}'. Falling back to file copy.", path);
            File.Copy(path, savePath, true);
        }

        return Task.CompletedTask;
    }

    private async Task CollectFromUrlAsync(
        Uri uri,
        string savePath,
        DownloadConfiguration configuration,
        CancellationToken ct)
    {
        logger.Debug("Downloading from '{Uri}' to '{Dest}'", uri, savePath);

        var config = new DownloaderConfig
        {
            BlockTimeout = configuration.TimeoutSeconds * 1000,
            MaxTryAgainOnFailure = configuration.MaxRetries,
            RequestConfiguration = { Proxy = configuration.Proxy }
        };

        await using var service = new DownloaderService(config);
        await service.DownloadFileTaskAsync(uri.ToString(), savePath, ct).ConfigureAwait(false);

        logger.Debug("Downloaded successfully to '{Dest}'", savePath);
    }

    #endregion
}


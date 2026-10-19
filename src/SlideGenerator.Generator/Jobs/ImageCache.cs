/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ImageCache.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SlideGenerator.Cloud;
using SlideGenerator.Image.Loading;
using SlideGenerator.Settings.Immutable;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Utilities;

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     One cache per workflow: dedupes inspections and downloads by the final resolved URI, validates each
///     download before caching, never retries a known failure within the run, prefetches in the background,
///     and is the single owner of the workflow's download folder.
/// </summary>
/// <remarks>
///     <para>
///         Inspections are deduplicated by <c>source.GetLeftPart(UriPartial.Query)</c>; downloads are
///         deduplicated by <c>info.Uri.AbsoluteUri.HashText()</c> — the FINAL resolved URI. Two different
///         source URIs whose inspections land on the same final URI share one download and one file.
///     </para>
///     <para>
///         All shared work uses the cache's own <see cref="CancellationToken" />; per-caller cancellation
///         only cancels that caller's <see cref="Task.WaitAsync(CancellationToken)" /> and never poisons
///         the shared <see cref="Lazy{T}" /> for other callers.
///     </para>
/// </remarks>
internal sealed class ImageCache(
    string folder,
    ICloudClient cloudClient,
    IImageLoader imageLoader,
    IHttpClientFactory httpClientFactory,
    ISettingProvider settingProvider,
    ILogger<ImageCache> logger) : IDisposable
{
    private const int MaxParallelDownloads = 4;

    private readonly ConcurrentDictionary<string, Lazy<Task<ContentInfo?>>> _inspections = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _downloads = new();
    private readonly SemaphoreSlim _downloadSlots = new(MaxParallelDownloads);
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    /// <summary>
    ///     Returns the local file holding the image behind <paramref name="source" />, or <see langword="null" />
    ///     when the resource is not an image, is unreachable, exceeds the size cap, or is undecodable.
    /// </summary>
    /// <param name="source">The image source URI as it appears in the worksheet cell.</param>
    /// <param name="onDownloading">Optional callback invoked exactly once, right before each distinct download starts.</param>
    /// <param name="ct">Token to cancel THIS caller's wait — does not cancel the shared work.</param>
    /// <exception cref="OperationCanceledException">Propagated when <paramref name="ct" /> is cancelled.</exception>
    public async Task<string?> GetImagePathAsync(Uri source, Action<Uri>? onDownloading, CancellationToken ct)
    {
        var key = Normalize(source);
        var inspection = _inspections.GetOrAdd(
            key,
            _ => new Lazy<Task<ContentInfo?>>(() => InspectAsync(source)));

        ContentInfo? info;
        try
        {
            info = await inspection.Value.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }

        if (info is null) return null;

        // Relaxed MIME filter: only reject known non-image types. Unknown / null / octet-stream still download —
        // validation after the download decides whether the bytes are usable.
        if (!IsAcceptableMime(info.MimeType)) return null;

        var target = info.Uri.AbsoluteUri.HashText();
        var download = _downloads.GetOrAdd(
            target,
            _ => new Lazy<Task<string?>>(() => DownloadAsync(info, target, onDownloading)));

        return await download.Value.WaitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Starts a best-effort background fetch for <paramref name="source" />. Swallows every failure
    ///     (including cancellation) and never produces an unobserved task exception. Safe to call repeatedly
    ///     with the same source — the underlying cache is shared.
    /// </summary>
    public void Prefetch(Uri source)
    {
        _ = PrefetchAsync(source);
    }

    /// <summary>
    ///     Cancels the cache's internal token and deletes the cache folder best-effort. Idempotent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed — nothing to do.
        }

        try
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to remove image cache folder '{Folder}'.", folder);
        }

        // _downloadSlots / _cts are deliberately not disposed: fire-and-forget prefetches may still be unwinding
        // and would throw ObjectDisposedException on Release()/Token. Neither holds an OS handle here.
    }

    /// <summary>
    ///     Deletes every immediate sub-directory of <paramref name="rootPath" /> (when it exists) whose name
    ///     is not in <paramref name="activeWorkflowIds" /> (case-insensitive). Each folder is removed
    ///     best-effort — a failed folder is logged and the next one continues.
    /// </summary>
    public static void DeleteOrphans(string rootPath, IReadOnlySet<string> activeWorkflowIds)
    {
        if (!Directory.Exists(rootPath)) return;

        string[] entries;
        try
        {
            entries = Directory.GetDirectories(rootPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Cannot even list — leave the rest alone.
            return;
        }

        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (activeWorkflowIds.Contains(name)) continue;

            try
            {
                Directory.Delete(entry, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best effort — log and continue with the next entry.
            }
        }
    }

    #region Private helpers

    /// <summary>
    ///     Strips the fragment and lowercases scheme/host so semantically equivalent URIs dedupe to the same
    ///     inspection entry.
    /// </summary>
    private static string Normalize(Uri source) => source.GetLeftPart(UriPartial.Query);

    /// <summary>
    ///     <see langword="true" /> when the MIME is unknown / null / octet-stream (download + validate), or
    ///     already an <c>image/*</c>; <see langword="false" /> only when the MIME is known and clearly
    ///     non-image (e.g. <c>text/html</c>).
    /// </summary>
    private static bool IsAcceptableMime(string? mime)
    {
        if (string.IsNullOrWhiteSpace(mime)) return true;
        if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return true;
        if (mime.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)) return true;
        if (mime.Equals("binary/octet-stream", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    ///     Inspects <paramref name="source" /> once and caches the result. Any non-cancellation failure is
    ///     logged and surfaced as <see langword="null" /> so callers do not retry.
    /// </summary>
    private async Task<ContentInfo?> InspectAsync(Uri source)
    {
        using var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
        var retry = settingProvider.Current.Network.Retry;
        try
        {
            return await Utilities.ExecuteWithBackoffAsync(
                retry.MaxRetries,
                TimeSpan.FromSeconds(retry.MaxRetryDelay),
                () => cloudClient.InspectAsync(source, httpClient, _cts.Token),
                _cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Inspect failed for {Source}.", source);
            return null;
        }
    }

    /// <summary>
    ///     Downloads the resource once into a sibling <c>.tmp</c>, validates it via
    ///     <see cref="IImageLoader.TryGetInfo(string, out IImageInfo?)" />, then moves it onto the final path.
    ///     A previously cached final file is returned as-is (resume path). A leftover <c>.tmp</c> is never
    ///     treated as a hit — it is overwritten on the next attempt. Any failure cleans up the tmp and
    ///     returns <see langword="null" /> so the shared Lazy records the failure for the rest of the run.
    /// </summary>
    private async Task<string?> DownloadAsync(ContentInfo info, string target, Action<Uri>? onDownloading)
    {
        var final = Path.Combine(folder, target);
        if (File.Exists(final)) return final;

        await _downloadSlots.WaitAsync(_cts.Token).ConfigureAwait(false);
        try
        {
            if (File.Exists(final)) return final; // a previous caller may have completed while we waited.

            Directory.CreateDirectory(folder);
            var tmp = final + ".tmp";
            onDownloading?.Invoke(info.Uri);

            var maxBytes = (long)settingProvider.Current.Network.MaxDownloadBytes;
            using var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
            var retry = settingProvider.Current.Network.Retry;

            var rejected = false;
            try
            {
                await Utilities.ExecuteWithBackoffAsync(
                    retry.MaxRetries,
                    TimeSpan.FromSeconds(retry.MaxRetryDelay),
                    async () =>
                    {
                        try
                        {
                            await cloudClient.DownloadToFileAsync(info.Uri, tmp, maxBytes, httpClient, _cts.Token)
                                .ConfigureAwait(false);
                            return tmp;
                        }
                        catch (InvalidDataException)
                        {
                            rejected = true;
                            return tmp;
                        }
                    },
                    _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                TryDelete(tmp);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Download failed for {Uri}.", info.Uri);
                TryDelete(tmp);
                return null;
            }

            if (rejected)
            {
                logger.LogWarning("Skipped download exceeding MaxDownloadBytes: {Uri}", info.Uri);
                TryDelete(tmp);
                return null;
            }

            // Validate the bytes are a real image BEFORE promoting to the final path.
            if (!imageLoader.TryGetInfo(tmp, out _))
            {
                logger.LogWarning("Downloaded file is not a valid image: {Uri}", info.Uri);
                TryDelete(tmp);
                return null;
            }

            File.Move(tmp, final, overwrite: true);
            return final;
        }
        finally
        {
            _downloadSlots.Release();
        }
    }

    private async Task PrefetchAsync(Uri source)
    {
        try
        {
            await GetImagePathAsync(source, null, _cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Prefetch failed for {Source}.", source);
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // best-effort
        }
        catch (UnauthorizedAccessException)
        {
            // best-effort
        }
    }

    #endregion
}

/// <summary>
///     Builds <see cref="ImageCache" /> instances scoped to a workflow's temp folder and exposes the
///     orphan-folder cleanup used at startup.
/// </summary>
internal sealed class ImageCacheFactory(
    ICloudClient cloudClient,
    IImageLoader imageLoader,
    IHttpClientFactory httpClientFactory,
    ISettingProvider settingProvider,
    ILoggerFactory loggerFactory)
{
    /// <summary>Creates a new cache rooted at <c>%TEMP%\SlideGenerator\{workflowId}</c>.</summary>
    public ImageCache Create(string workflowId)
    {
        var folder = Path.Combine(NameAndPaths.TempFolder.RootPath, workflowId);
        return new ImageCache(
            folder,
            cloudClient,
            imageLoader,
            httpClientFactory,
            settingProvider,
            loggerFactory.CreateLogger<ImageCache>());
    }

    /// <summary>Removes every workflow folder under the temp root that does not belong to an active workflow.</summary>
    public void DeleteOrphans(IReadOnlySet<string> activeWorkflowIds)
    {
        ImageCache.DeleteOrphans(NameAndPaths.TempFolder.RootPath, activeWorkflowIds);
    }
}
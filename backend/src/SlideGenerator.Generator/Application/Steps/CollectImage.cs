/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: CollectImage.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Image.Application.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using HardLink = SlideGenerator.Utilities.HardLink;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Acquires a single image from a direct URL or local file path to a local temporary path.
///     For URLs, applies a three-step pipeline: redirect-following inspect → cloud-resolve
///     (if applicable) → image-check inspect → download.
///     Implements idempotency by skipping existing valid files.
///     Deduplicates identical URLs: the owner step downloads; waiters hard-link the result.
/// </summary>
public sealed class CollectImage(
    ICloudClient cloudClient,
    ICloudResolver cloudResolver,
    IImageFactory imageFactory,
    IGateLocker<GateType> gateLocker,
    IHttpClientFactory httpClientFactory) : StepBodyAsync
{
    /// <summary>
    ///     The image acquisition task to process.
    ///     Mapped from the <c>ForEach</c> loop in the workflow.
    /// </summary>
    public ImageContext Task { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var logger = data.LoggerFactory!.CreateLogger(nameof(CollectImage));

        if (Task.SourceUrl == null)
        {
            logger.LogWarning("URL is not valid for row {RowIndex}, column {ColumnName}. Skipping.",
                Task.RowIndex, Task.ColumnName);
            return ExecutionResult.Next();
        }

        var enlistResult = data.AssetCoordinator!.Enlist(Task.SourceUrl);

        return enlistResult switch
        {
            OwnerEnlistment owner => await RunOwnerAsync(context, data, logger, owner).ConfigureAwait(false),
            WaiterEnlistment waiter => await RunWaiterAsync(data, logger, waiter.WaitTask).ConfigureAwait(false),
            _ => ExecutionResult.Next()
        };
    }

    #region Private helpers

    /// <summary>
    ///     Runs the three-step URL resolution pipeline and returns the final image <see cref="Uri" />
    ///     to download, or <see langword="null" /> when the URL cannot be resolved to a downloadable image.
    ///     <list type="number">
    ///         <item>Inspect the raw URL to follow redirects and obtain the final URI.</item>
    ///         <item>If the final URI belongs to a cloud provider, resolve it to a direct download URI.</item>
    ///         <item>Inspect the resolved URI and return it only when the content-type is an image.</item>
    ///     </list>
    /// </summary>
    private async Task<Uri?> ResolveSourceAsync(HttpClient httpClient, CancellationToken ct)
    {
        var raw = Task.SourceUrl!;
        if (!raw.Contains("://")) raw = "https://" + raw;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return null;

        // Step 1: inspect → follow redirects, get final URI
        var info = await cloudClient.InspectAsync(uri, httpClient, ct).ConfigureAwait(false);
        if (info == null) return null;
        uri = info.Uri;

        // Step 2: cloud-resolve if applicable
        if (cloudResolver.GetCloudHost(uri.ToString(), out _))
        {
            var resolved = await cloudResolver
                .ResolveAsync(uri.ToString(), httpClient, ct)
                .ConfigureAwait(false);
            if (resolved == null) return null;
            uri = resolved;
        }

        // Step 3: inspect resolved URI → return only when content is an image
        var resolvedInfo = await cloudClient.InspectAsync(uri, httpClient, ct).ConfigureAwait(false);
        return resolvedInfo?.IsImage() == true ? resolvedInfo.Uri : null;
    }

    #endregion

    #region Primary / Secondary

    private async Task<ExecutionResult> RunOwnerAsync(
        IStepExecutionContext context,
        GeneratingContext data,
        ILogger logger,
        OwnerEnlistment owner)
    {
        var ct = context.CancellationToken;
        var notified = false;

        // Idempotency: skip if file already exists and is valid
        if (File.Exists(Task.DownloadPath))
            try
            {
                using var testImage = imageFactory.Open(Task.DownloadPath);
                logger.LogDebug("Image for row {RowIndex} already exists and is valid, skipping acquisition",
                    Task.RowIndex);
                owner.SubmitResult(Task.DownloadPath);
                return ExecutionResult.Next();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Image corrupt, retrying acquisition | Row: {RowIndex}", Task.RowIndex);
                File.Delete(Task.DownloadPath);
            }

        var dir = Path.GetDirectoryName(Task.DownloadPath);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        await gateLocker.AcquireAsync(GateType.DownloadImage, ct).ConfigureAwait(false);
        try
        {
            logger.LogDebug("Acquiring image for row {RowIndex} from '{Url}'", Task.RowIndex, Task.SourceUrl);
            var result = await FetchToPathAsync(data, logger, ct).ConfigureAwait(false);
            if (result != null)
                logger.LogDebug("Image acquired | Row: {RowIndex}, Column: {ColumnName}",
                    Task.RowIndex, Task.ColumnName);
            owner.SubmitResult(result);
            notified = true;
        }
        catch (Exception ex) when (ex is not NullReferenceException
                                       and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            logger.LogError(ex, "Acquisition failed | Row: {RowIndex}, Column: {ColumnName}",
                Task.RowIndex, Task.ColumnName);
            owner.SubmitException(ex);
            notified = true;
        }
        finally
        {
            gateLocker.Release(GateType.DownloadImage);
            if (!notified)
                owner.SubmitException(
                    new WorkflowFaultedException("CollectImage primary faulted before completing.", null));
        }

        return ExecutionResult.Next();
    }

    private async Task<string?> FetchToPathAsync(GeneratingContext data, ILogger logger, CancellationToken ct)
    {
        if (File.Exists(Task.SourceUrl!))
        {
            if (!data.Request.AllowLocalImagePaths)
            {
                logger.LogWarning("Source '{Url}' is a local file but local paths are not allowed. Skipping.",
                    Task.SourceUrl);
                return null;
            }

            File.Copy(Task.SourceUrl!, Task.DownloadPath, true);
            return Task.DownloadPath;
        }

        // URL path — 3-step pipeline: inspect → cloud-resolve → inspect → download
        using var httpClient = httpClientFactory.CreateClient();
        var resolvedUri = await ResolveSourceAsync(httpClient, ct).ConfigureAwait(false);

        if (resolvedUri == null)
        {
            logger.LogWarning("URL '{Url}' did not resolve to a downloadable image. Skipping.", Task.SourceUrl);
            return null;
        }

        await cloudClient.DownloadAsync(resolvedUri, Task.DownloadPath, httpClient, ct).ConfigureAwait(false);
        return Task.DownloadPath;
    }

    private async Task<ExecutionResult> RunWaiterAsync(
        GeneratingContext data, ILogger logger, Task<string?> waitTask)
    {
        // Idempotency: own file already exists (resume scenario)
        if (File.Exists(Task.DownloadPath))
        {
            logger.LogDebug(
                "Image for row {RowIndex} already exists (resume), skipping secondary hardlink", Task.RowIndex);
            return ExecutionResult.Next();
        }

        var primaryPath = await waitTask.ConfigureAwait(false);

        if (primaryPath != null && File.Exists(primaryPath))
        {
            var dir = Path.GetDirectoryName(Task.DownloadPath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            HardLink.Create(Task.DownloadPath, primaryPath);
            logger.LogDebug(
                "Hard-linked image for row {RowIndex} from primary path '{PrimaryPath}'",
                Task.RowIndex, primaryPath);
        }
        else
        {
            logger.LogWarning(
                "Primary acquisition failed for URL '{Url}', row {RowIndex} will have no image",
                Task.SourceUrl, Task.RowIndex);
        }

        return ExecutionResult.Next();
    }

    #endregion
}
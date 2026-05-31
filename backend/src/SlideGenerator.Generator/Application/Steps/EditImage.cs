/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: EditImage.cs
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

using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Image.Application;
using SlideGenerator.Image.Application.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using HardLink = SlideGenerator.Utilities.HardLink;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Processes a single image by cropping and resizing it to match
///     the target shape dimensions using an intelligent ROI algorithm.
///     Deduplicates identical (URI, options, size) combinations: only one step edits;
///     others hard-link the result.
/// </summary>
public sealed class EditImage(
    IRoiResolver roiResolver,
    IImageFactory imageFactory,
    IGateLocker<GateType> gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     The editing task to process.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ImageContext Task { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var logger = data.LoggerFactory!.CreateLogger(nameof(EditImage));

        var finalEditPath = Task.EditPath + ".png";

        // Idempotency: skip if the file already exists
        if (File.Exists(finalEditPath))
        {
            logger.LogDebug("Edited image for row {RowIndex} already exists at '{Path}', skipping edit",
                Task.RowIndex, finalEditPath);
            return ExecutionResult.Next();
        }

        // Skip if SourceUrl is null and no FallbackImagePath is provided
        if (Task.SourceUrl == null && string.IsNullOrWhiteSpace(Task.FallbackImagePath))
        {
            logger.LogDebug("No source URI or fallback image for row {RowIndex}, shape {ShapeName}. Skipping.",
                Task.RowIndex, Task.ShapeName);
            return ExecutionResult.Next();
        }

        var editKey = BuildEditKey();
        var enlistResult = data.AssetCoordinator!.Enlist(editKey);

        return enlistResult switch
        {
            OwnerEnlistment owner => await RunOwnerAsync(context, data, logger, owner, finalEditPath)
                .ConfigureAwait(false),
            WaiterEnlistment waiter => await RunWaiterAsync(data, logger, waiter.WaitTask, finalEditPath)
                .ConfigureAwait(false),
            _ => ExecutionResult.Next()
        };
    }

    private async Task<ExecutionResult> RunOwnerAsync(
        IStepExecutionContext context, GeneratingContext data, ILogger logger,
        OwnerEnlistment owner, string finalEditPath)
    {
        var ct = context.CancellationToken;

        var sourceFile = FindSourceFile(logger);
        if (sourceFile == null)
        {
            owner.SubmitResult(null);
            return ExecutionResult.Next();
        }

        var editDir = Path.GetDirectoryName(finalEditPath);
        if (editDir != null && !Directory.Exists(editDir)) Directory.CreateDirectory(editDir);

        var notified = false;
        await gateLocker.AcquireAsync(GateType.EditImage, ct).ConfigureAwait(false);
        try
        {
            try
            {
                await ProcessAndSaveImageAsync(sourceFile, finalEditPath, data, logger, ct).ConfigureAwait(false);
                owner.SubmitResult(finalEditPath);
                notified = true;
            }
            catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                           and not IndexOutOfRangeException)
            {
                logger.LogError(ex, "Edit image failed for '{Path}'", Path.GetFileName(finalEditPath));
                owner.SubmitException(ex);
                notified = true;
            }
        }
        finally
        {
            gateLocker.Release(GateType.EditImage);
            // NRE/ICE/IOOR bypass inner catch and re-throw into WorkflowCore lifecycle —
            // fault secondaries first so they don't hang.
            if (!notified)
                owner.SubmitException(
                    new WorkflowFaultedException("EditImage primary faulted before completing.", null));
        }

        return ExecutionResult.Next();
    }

    private string? FindSourceFile(ILogger logger)
    {
        var downloadDir = Path.GetDirectoryName(Task.DownloadPath);
        var downloadPrefix = Path.GetFileName(Task.DownloadPath);
        string? sourceFile = null;

        if (downloadDir != null && Directory.Exists(downloadDir))
            sourceFile = Directory.GetFiles(downloadDir, $"{downloadPrefix}.*").FirstOrDefault();

        if (sourceFile != null && File.Exists(sourceFile))
            return sourceFile;

        if (!string.IsNullOrWhiteSpace(Task.FallbackImagePath) && File.Exists(Task.FallbackImagePath))
        {
            logger.LogDebug("Primary source missing for row {RowIndex}, using fallback: '{Fallback}'",
                Task.RowIndex, Task.FallbackImagePath);
            return Task.FallbackImagePath;
        }

        if (Task.SourceUrl != null)
            logger.LogError(
                new FileNotFoundException("Source image and fallback not found for editing.", Task.DownloadPath),
                "Missing source for row {RowIndex} prefix '{Prefix}'", Task.RowIndex, downloadPrefix);

        return null;
    }

    private async Task ProcessAndSaveImageAsync(
        string sourceFile, string finalEditPath, GeneratingContext data, ILogger logger, CancellationToken ct)
    {
        logger.LogDebug("Editing image '{Source}' for shape '{ShapeName}' (Row {RowIndex})", sourceFile,
            Task.ShapeName, Task.RowIndex);

        using var image = imageFactory.Open(sourceFile);
        var targetSize = new Size((int)Math.Round(Task.Width), (int)Math.Round(Task.Height));

        logger.LogDebug("Calculating ROI for row {RowIndex} using {Algorithm}", Task.RowIndex,
            Task.EditOptions.RoiOption);

        var roi = await roiResolver.CalculateRoiAsync(image, targetSize, Task.EditOptions.RoiOption)
            .ConfigureAwait(false);

        logger.LogDebug("Applying crop {ROI} to image for row {RowIndex}", roi, Task.RowIndex);
        image.Crop(roi);

        var currentSize = new Size((int)image.Info.Width, (int)image.Info.Height);
        var maxAspectSize = currentSize.GetMaxAspectSize(targetSize);

        logger.LogDebug("Resizing image for row {RowIndex} to {Size}", Task.RowIndex, maxAspectSize);
        image.Resize(maxAspectSize);

        await image.WriteAsync(finalEditPath).ConfigureAwait(false);

        logger.LogDebug("Image edited | Row: {RowIndex}, Shape: {ShapeName}", Task.RowIndex, Task.ShapeName);

        if (data.Request.DownloadAssetsPath == null && sourceFile != Task.FallbackImagePath)
            try
            {
                File.Delete(sourceFile);
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "Temp file cleanup skipped | Path: {Path}", sourceFile);
            }
    }

    private async Task<ExecutionResult> RunWaiterAsync(
        GeneratingContext data, ILogger logger, Task<string?> waitTask, string finalEditPath)
    {
        // Idempotency: own edit file already exists (resume scenario)
        if (File.Exists(finalEditPath))
        {
            logger.LogDebug(
                "Edited image for row {RowIndex} already exists (resume), skipping secondary hardlink",
                Task.RowIndex);
            return ExecutionResult.Next();
        }

        var primaryPath = await waitTask.ConfigureAwait(false);

        if (primaryPath != null && File.Exists(primaryPath))
        {
            var editDir = Path.GetDirectoryName(finalEditPath);
            if (editDir != null && !Directory.Exists(editDir)) Directory.CreateDirectory(editDir);

            HardLink.Create(finalEditPath, primaryPath);
            logger.LogDebug(
                "Hard-linked edit for row {RowIndex}, shape {ShapeName} from primary path '{PrimaryPath}'",
                Task.RowIndex, Task.ShapeName, primaryPath);

            // Clean up own source download file if using temp path (consistent with primary behavior)
            if (data.Request.DownloadAssetsPath == null)
            {
                var downloadDir = Path.GetDirectoryName(Task.DownloadPath);
                var downloadPrefix = Path.GetFileName(Task.DownloadPath);
                if (downloadDir != null && Directory.Exists(downloadDir))
                {
                    var sourceFile = Directory.GetFiles(downloadDir, $"{downloadPrefix}.*").FirstOrDefault();
                    if (sourceFile != null && sourceFile != Task.FallbackImagePath)
                        try
                        {
                            File.Delete(sourceFile);
                        }
                        catch (Exception ex)
                        {
                            logger.LogTrace(ex, "Temp file cleanup skipped | Path: {Path}", sourceFile);
                        }
                }
            }
        }
        else
        {
            logger.LogWarning(
                "Primary edit failed for row {RowIndex}, shape {ShapeName} will have no image",
                Task.RowIndex, Task.ShapeName);
        }

        return ExecutionResult.Next();
    }

    /// <summary>
    ///     Builds a deduplication key encoding all inputs that determine the edit output:
    ///     source URI, fallback path, edit options, and target dimensions.
    /// </summary>
    private string BuildEditKey()
    {
        return
            $"{Task.SourceUrl}|{Task.FallbackImagePath}|{Task.EditOptions}|{(int)Math.Round(Task.Width)}x{(int)Math.Round(Task.Height)}";
    }
}
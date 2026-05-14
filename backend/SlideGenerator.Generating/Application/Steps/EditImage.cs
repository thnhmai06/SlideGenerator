/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Generating.Domain.Models.Contexts;
using SlideGenerator.Image.Application;
using SlideGenerator.Image.Application.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using HardLink = SlideGenerator.Common.Utilities.HardLink;

namespace SlideGenerator.Generating.Application.Steps;

/// <summary>
///     Processes a single image by cropping and resizing it to match
///     the target shape dimensions using an intelligent ROI algorithm.
///     Deduplicates identical (URI, options, size) combinations: only one step edits;
///     others hard-link the result.
/// </summary>
public sealed class EditImage(
    IRoiResolver roiResolver,
    IImageFactory imageFactory,
    IGateLocker gateLocker) : StepBodyAsync
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
        using var scope = data.Logger!.BeginScope("EditImage");

        var finalEditPath = Task.EditPath + ".png";

        // Idempotency: skip if the file already exists
        if (File.Exists(finalEditPath))
        {
            data.Logger.Debug("Edited image for row {RowIndex} already exists at '{Path}', skipping edit",
                Task.RowIndex, finalEditPath);
            return ExecutionResult.Next();
        }

        // Skip if SourceUri is null and no FallbackImagePath is provided
        if (Task.SourceUri == null && string.IsNullOrWhiteSpace(Task.FallbackImagePath))
        {
            data.Logger.Debug("No source URI or fallback image for row {RowIndex}, shape {ShapeName}. Skipping.",
                Task.RowIndex, Task.ShapeName);
            return ExecutionResult.Next();
        }

        var editKey = BuildEditKey();
        var enlistResult = data.AssetCoordinator!.Enlist(editKey);

        return enlistResult switch
        {
            PrimaryEnlistment primary => await RunPrimaryAsync(data, primary.SubmitResult, finalEditPath)
                .ConfigureAwait(false),
            SecondaryEnlistment secondary => await RunSecondaryAsync(data, secondary.WaitTask, finalEditPath)
                .ConfigureAwait(false),
            _ => ExecutionResult.Next()
        };
    }

    private async Task<ExecutionResult> RunPrimaryAsync(
        GeneratingContext data, Action<string?> complete, string finalEditPath)
    {
        // Discover the source file
        string? sourceFile = null;
        var downloadDir = Path.GetDirectoryName(Task.DownloadPath);
        var downloadPrefix = Path.GetFileName(Task.DownloadPath);

        if (downloadDir != null && Directory.Exists(downloadDir))
            sourceFile = Directory.GetFiles(downloadDir, $"{downloadPrefix}.*").FirstOrDefault();

        // Use fallback if primary source is missing
        if (sourceFile == null || !File.Exists(sourceFile))
        {
            if (!string.IsNullOrWhiteSpace(Task.FallbackImagePath) && File.Exists(Task.FallbackImagePath))
            {
                sourceFile = Task.FallbackImagePath;
                data.Logger!.Debug("Primary source missing for row {RowIndex}, using fallback: '{Fallback}'",
                    Task.RowIndex, sourceFile);
            }
            else
            {
                if (Task.SourceUri != null)
                    using (data.Logger!.BeginScope(downloadPrefix))
                    {
                        data.Logger.Error(
                            new FileNotFoundException("Source image and fallback not found for editing.",
                                Task.DownloadPath), "Missing source");
                    }

                complete(null);
                return ExecutionResult.Next();
            }
        }

        var editDir = Path.GetDirectoryName(finalEditPath);
        if (editDir != null && !Directory.Exists(editDir)) Directory.CreateDirectory(editDir);

        await gateLocker.AcquireAsync(GateType.EditImage).ConfigureAwait(false);
        try
        {
            data.Logger!.Debug("Editing image '{Source}' for shape '{ShapeName}' (Row {RowIndex})", sourceFile,
                Task.ShapeName, Task.RowIndex);

            using var image = imageFactory.Open(sourceFile);
            var targetSize = new Size((int)Math.Round(Task.Width), (int)Math.Round(Task.Height));

            data.Logger.Debug("Calculating ROI for row {RowIndex} using {Algorithm}", Task.RowIndex,
                Task.EditOptions.RoiOption);

            var roi = await roiResolver.CalculateRoiAsync(
                image,
                targetSize,
                Task.EditOptions.RoiOption).ConfigureAwait(false);

            data.Logger.Debug("Applying crop {ROI} to image for row {RowIndex}", roi, Task.RowIndex);
            image.Crop(roi);

            var currentSize = new Size((int)image.Width, (int)image.Height);
            var maxAspectSize = currentSize.GetMaxAspectSize(targetSize);

            data.Logger.Debug("Resizing image for row {RowIndex} to {Size}", Task.RowIndex, maxAspectSize);
            image.Resize(maxAspectSize);

            await image.WriteAsync(finalEditPath).ConfigureAwait(false);

            data.Logger.Information("Successfully edited image for row {RowIndex}, shape {ShapeName}", Task.RowIndex,
                Task.ShapeName);

            complete(finalEditPath);

            if (data.Request.DownloadAssetsPath == null && sourceFile != Task.FallbackImagePath)
                try
                {
                    File.Delete(sourceFile);
                }
                catch
                {
                    /* ignore */
                }
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            using (data.Logger!.BeginScope(Path.GetFileName(finalEditPath)))
            {
                data.Logger.Error(ex, "Edit image failed");
            }

            complete(null);
        }
        finally
        {
            gateLocker.Release(GateType.EditImage);
        }

        return ExecutionResult.Next();
    }

    private async Task<ExecutionResult> RunSecondaryAsync(
        GeneratingContext data, Task<string?> waitTask, string finalEditPath)
    {
        // Idempotency: own edit file already exists (resume scenario)
        if (File.Exists(finalEditPath))
        {
            data.Logger!.Debug(
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
            data.Logger!.Debug(
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
                        catch
                        {
                            /* ignore */
                        }
                }
            }
        }
        else
        {
            data.Logger!.Warning(
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
            $"{Task.SourceUri?.AbsoluteUri}|{Task.FallbackImagePath}|{Task.EditOptions}|{(int)Math.Round(Task.Width)}x{(int)Math.Round(Task.Height)}";
    }
}
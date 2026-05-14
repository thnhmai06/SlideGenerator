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

namespace SlideGenerator.Generating.Application.Steps;

/// <summary>
///     Processes a single image by cropping and resizing it to match
///     the target shape dimensions using an intelligent ROI algorithm.
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
        using var scope = data.Logger.BeginScope("EditImage");

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

        // Discover the source file
        string? sourceFile = null;
        var downloadDir = Path.GetDirectoryName(Task.DownloadPath);
        var downloadPrefix = Path.GetFileName(Task.DownloadPath);

        if (downloadDir != null && Directory.Exists(downloadDir))
            sourceFile = Directory.GetFiles(downloadDir, $"{downloadPrefix}.*").FirstOrDefault();

        // Use fallback if a primary source is missing
        if (sourceFile == null || !File.Exists(sourceFile))
        {
            if (!string.IsNullOrWhiteSpace(Task.FallbackImagePath) && File.Exists(Task.FallbackImagePath))
            {
                sourceFile = Task.FallbackImagePath;
                data.Logger.Debug("Primary source missing for row {RowIndex}, using fallback: '{Fallback}'",
                    Task.RowIndex, sourceFile);
            }
            else
            {
                // Source is missing and no fallback available
                if (Task.SourceUri != null)
                    using (data.Logger.BeginScope(downloadPrefix))
                    {
                        data.Logger.Error(
                            new FileNotFoundException("Source image and fallback not found for editing.",
                                Task.DownloadPath), "Missing source");
                    }

                return ExecutionResult.Next();
            }
        }

        // Ensure target directory exists
        var editDir = Path.GetDirectoryName(finalEditPath);
        if (editDir != null && !Directory.Exists(editDir)) Directory.CreateDirectory(editDir);

        await gateLocker.AcquireAsync(GateType.EditImage).ConfigureAwait(false);
        try
        {
            data.Logger.Debug("Editing image '{Source}' for shape '{ShapeName}' (Row {RowIndex})", sourceFile,
                Task.ShapeName, Task.RowIndex);

            using var image = imageFactory.Open(sourceFile);
            var targetSize = new Size((int)Math.Round(Task.Width), (int)Math.Round(Task.Height));

            // 1. Calculate ROI based on the selected algorithm
            data.Logger.Debug("Calculating ROI for row {RowIndex} using {Algorithm}", Task.RowIndex,
                Task.EditOptions.RoiOption);

            var roi = await roiResolver.CalculateRoiAsync(
                image,
                targetSize,
                Task.EditOptions.RoiOption).ConfigureAwait(false);

            // 2. Crop the image to the ROI
            data.Logger.Debug("Applying crop {ROI} to image for row {RowIndex}", roi, Task.RowIndex);
            image.Crop(roi);

            // 3. Resize with a maintained aspect ratio to fit the target shape dimensions
            var currentSize = new Size((int)image.Width, (int)image.Height);
            var maxAspectSize = currentSize.GetMaxAspectSize(targetSize);

            data.Logger.Debug("Resizing image for row {RowIndex} to {Size}", Task.RowIndex, maxAspectSize);
            image.Resize(maxAspectSize);

            // 4. Save the edited image as PNG
            await image.WriteAsync(finalEditPath).ConfigureAwait(false);

            data.Logger.Information("Successfully edited image for row {RowIndex}, shape {ShapeName}", Task.RowIndex,
                Task.ShapeName);

            // Delete raw download image when using the default (temporary) assets path
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
            using (data.Logger.BeginScope(Path.GetFileName(finalEditPath)))
            {
                data.Logger.Error(ex, "Edit image failed");
            }
        }
        finally
        {
            gateLocker.Release(GateType.EditImage);
        }

        return ExecutionResult.Next();
    }
}

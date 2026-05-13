/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: DownloadImage.cs
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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Download.Application.Abstractions;
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Generating.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generating.Application.Steps;

/// <summary>
///     Downloads a single image from a cloud URI to a local temporary path.
///     Implements idempotency by skipping existing files.
/// </summary>
public sealed class DownloadImage(
    IDownloadService downloadService,
    ICloudResolver multiCloudResolver,
    IImageFactory imageFactory,
    IGateLocker gateLocker,
    HttpClient httpClient) : StepBodyAsync
{
    /// <summary>
    ///     The download task to process.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ImageContext Task { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        using var scope = data.Logger.BeginScope("DownloadImage");

        // Idempotency: skip if file already exists and is valid
        if (File.Exists(Task.DownloadPath))
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(Task.DownloadPath).ConfigureAwait(false);
                using var testImage = imageFactory.Open(bytes);
                data.Logger.Debug("Image for row {RowIndex} already exists and is valid, skipping download", Task.RowIndex);
                return ExecutionResult.Next();
            }
            catch
            {
                data.Logger.Warning("Existing image for row {RowIndex} is corrupt. Retrying download.", Task.RowIndex);
                File.Delete(Task.DownloadPath);
            }
        }

        // Ensure directory exists
        var dir = Path.GetDirectoryName(Task.DownloadPath);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        await gateLocker.AcquireAsync(GateType.DownloadImage).ConfigureAwait(false);
        try
        {
            if (Task.SourceUri == null)
            {
                var path = $"Row{Task.RowIndex}_{Task.ColumnName}";
                using (data.Logger.BeginScope(path))
                    data.Logger.Warning("URI is not valid. Skipping.");
                return ExecutionResult.Next();
            }

            data.Logger.Debug("Downloading image for row {RowIndex} from '{Uri}'", Task.RowIndex, Task.SourceUri);

            var resolvedUri =
                await multiCloudResolver.ResolveUriAsync(Task.SourceUri, httpClient).ConfigureAwait(false);

            data.Logger.Debug("Resolved URI for row {RowIndex}: '{ResolvedUri}'", Task.RowIndex, resolvedUri);

            await downloadService.DownloadAsync(resolvedUri, Task.DownloadPath).ConfigureAwait(false);

            data.Logger.Information("Successfully downloaded image for row {RowIndex}, column {ColumnName}",
                Task.RowIndex, Task.ColumnName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            var path = $"Row{Task.RowIndex}_{Task.ColumnName}";
            using (data.Logger.BeginScope(path))
                data.Logger.Error(ex, "Download failed");
        }
        finally
        {
            gateLocker.Release(GateType.DownloadImage);
        }

        return ExecutionResult.Next();
    }
}






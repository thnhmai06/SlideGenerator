/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Pipeline
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

using Serilog;
using SlideGenerator.Cloud.Services;
using SlideGenerator.Coordinator.Models;
using SlideGenerator.Coordinator.Services;
using SlideGenerator.Download.Services;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Pipeline.Generating.Steps;

/// <summary>
///     Downloads a single image from a cloud URI to a local temporary path.
///     Implements idempotency by skipping existing files.
/// </summary>
public sealed class DownloadImage(
    DownloadService downloadService,
    MultiCloudResolver multiCloudResolver,
    GateLocker gateLocker,
    HttpClient httpClient,
    ILogger logger) : StepBodyAsync
{
    /// <summary>
    ///     The download task to process.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ImageTask Task { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingTask)context.Workflow.Data;
        data.TryInitLogger(logger, context.Workflow.Id);

        // Idempotency: skip if file already exists and is valid
        if (File.Exists(Task.DownloadPath))
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(Task.DownloadPath).ConfigureAwait(false);
                using var testImage = Image.Utilities.Decode(bytes);
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
                data.Logger.ForContext("Path", path).Warning("URI is not valid. Skipping.");
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
            data.Logger.ForContext("Path", path).Error(ex, "Download failed");
        }
        finally
        {
            gateLocker.Release(GateType.DownloadImage);
        }

        return ExecutionResult.Next();
    }
}
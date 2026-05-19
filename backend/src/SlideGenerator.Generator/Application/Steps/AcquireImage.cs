/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: AcquireImage.cs
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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Settings.Application.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using HardLink = SlideGenerator.Utilities.Helper.HardLink;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Acquires a single image from a URL or local file path to a local temporary path.
///     Implements idempotency by skipping existing valid files.
///     Deduplicates identical URLs: only one step downloads; others hard-link the result.
/// </summary>
public sealed class AcquireImage(
    IImageAcquirer imageAcquirer,
    IImageFactory imageFactory,
    IGateLocker gateLocker,
    ISettingProvider settingProvider) : StepBodyAsync
{
    /// <summary>
    ///     The acquisition task to process.
    ///     Mapped from the ForEach loop in the workflow.
    /// </summary>
    public ImageContext Task { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        using var scope = data.Logger!.BeginScope("AcquireImage");

        if (Task.SourceUrl == null)
        {
            var path = $"Row{Task.RowIndex}_{Task.ColumnName}";
            using (data.Logger.BeginScope(path))
            {
                data.Logger.Warning("URL is not valid. Skipping.");
            }

            return ExecutionResult.Next();
        }

        var enlistResult = data.AssetCoordinator!.Enlist(Task.SourceUrl);

        return enlistResult switch
        {
            PrimaryEnlistment primary => await RunPrimaryAsync(context, data, primary).ConfigureAwait(false),
            SecondaryEnlistment secondary => await RunSecondaryAsync(data, secondary.WaitTask).ConfigureAwait(false),
            _ => ExecutionResult.Next()
        };
    }

    private async Task<ExecutionResult> RunPrimaryAsync(IStepExecutionContext context, GeneratingContext data,
        PrimaryEnlistment primary)
    {
        var ct = context.CancellationToken;
        var notified = false;

        // Idempotency: skip if file already exists and is valid
        if (File.Exists(Task.DownloadPath))
            try
            {
                using var testImage = imageFactory.Open(Task.DownloadPath);
                data.Logger!.Debug("Image for row {RowIndex} already exists and is valid, skipping acquisition",
                    Task.RowIndex);
                primary.SubmitResult(Task.DownloadPath);
                return ExecutionResult.Next();
            }
            catch
            {
                data.Logger!.Warning("Existing image for row {RowIndex} is corrupt. Retrying acquisition.",
                    Task.RowIndex);
                File.Delete(Task.DownloadPath);
            }

        var dir = Path.GetDirectoryName(Task.DownloadPath);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        await gateLocker.AcquireAsync(GateType.DownloadImage, ct).ConfigureAwait(false);
        try
        {
            data.Logger!.Debug("Acquiring image for row {RowIndex} from '{Url}'", Task.RowIndex, Task.SourceUrl);

            var net = settingProvider.Current.Network;
            var config = new DownloadConfiguration
            {
                MaxRetries = net.Retry.MaxRetries,
                TimeoutSeconds = net.Retry.Timeout,
                Proxy = net.Proxy.GetWebProxy()
            };

            await imageAcquirer.AcquireAsync(
                Task.SourceUrl!, Task.DownloadPath, config,
                data.Request.AllowLocalImagePaths, ct).ConfigureAwait(false);

            data.Logger.Information("Successfully acquired image for row {RowIndex}, column {ColumnName}",
                Task.RowIndex, Task.ColumnName);
            primary.SubmitResult(Task.DownloadPath);
            notified = true;
        }
        catch (Exception ex) when (ex is not NullReferenceException
                                       and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            var path = $"Row{Task.RowIndex}_{Task.ColumnName}";
            using (data.Logger!.BeginScope(path))
            {
                data.Logger.Error(ex, "Acquisition failed");
            }

            primary.SubmitException(ex);
            notified = true;
        }
        finally
        {
            gateLocker.Release(GateType.DownloadImage);
            // NRE/ICE/IOOR re-throw straight through to WorkflowCore for persistence handling,
            // but secondaries must not hang — fault their wait tasks first.
            if (!notified)
                primary.SubmitException(
                    new WorkflowFaultedException("AcquireImage primary faulted before completing.", null));
        }

        return ExecutionResult.Next();
    }

    private async Task<ExecutionResult> RunSecondaryAsync(GeneratingContext data, Task<string?> waitTask)
    {
        // Idempotency: own file already exists (resume scenario)
        if (File.Exists(Task.DownloadPath))
        {
            data.Logger!.Debug(
                "Image for row {RowIndex} already exists (resume), skipping secondary hardlink", Task.RowIndex);
            return ExecutionResult.Next();
        }

        var primaryPath = await waitTask.ConfigureAwait(false);

        if (primaryPath != null && File.Exists(primaryPath))
        {
            var dir = Path.GetDirectoryName(Task.DownloadPath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            HardLink.Create(Task.DownloadPath, primaryPath);
            data.Logger!.Debug(
                "Hard-linked image for row {RowIndex} from primary path '{PrimaryPath}'",
                Task.RowIndex, primaryPath);
        }
        else
        {
            data.Logger!.Warning(
                "Primary acquisition failed for URL '{Url}', row {RowIndex} will have no image",
                Task.SourceUrl, Task.RowIndex);
        }

        return ExecutionResult.Next();
    }
}
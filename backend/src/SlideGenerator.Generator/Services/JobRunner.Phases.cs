/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobRunner.Phases.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Cloud;
using SlideGenerator.Document.Slides;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Generator.Models.Data;
using SlideGenerator.Generator.Models.Enum;
using SlideGenerator.Image.Loading;
using SlideGenerator.Recipe.Models.Components;
using SlideGenerator.Utilities;

namespace SlideGenerator.Generator.Services;

internal sealed partial class JobRunner
{
    /// <summary>
    ///     Runs phases A→D in order, skipping phases already completed on resume, checkpointing pause/cancel
    ///     between rows and at every phase boundary. Returns the final <see cref="JobRecord" /> (Status =
    ///     Complete, Phase = Done) once every row has passed through every phase.
    /// </summary>
    private async Task<JobRecord> RunPhasesAsync(JobRecord initial, RunningJob running, ILogger jobLogger)
    {
        var requestId = initial.RequestId;
        var jobId = initial.JobId;
        var spec = initial.Specification;
        var ct = running.Cts.Token;

        using var workbook = await workbookOpener.OpenWorkbookReadOnlyAsync(
            new WorkbookIdentifier(spec.WorkbookPath), ct).ConfigureAwait(false);
        var worksheet = workbook.GetWorksheet(spec.WorksheetName)
            ?? throw new InvalidOperationException($"Sheet '{spec.WorksheetName}' not found in '{spec.WorkbookPath}'.");

        var headerToIndex = Utilities.BuildHeaderToIndexMap(worksheet);
        var dataCount = worksheet.RowCount - 1;
        var dataRows = (spec.RowFilter?.GetIndices(dataCount) ?? Enumerable.Range(1, dataCount)).ToList();

        // Phase A — create/open output.
        var templateId = new PresentationIdentifier(spec.TemplatePresentationPath);
        var output = initial.Phase == JobPhase.CreatingOutput && !File.Exists(spec.OutputPath)
            ? await CreateOutputAsync(spec, templateId, jobLogger, ct).ConfigureAwait(false)
            : await OpenOutputAsync(new PresentationIdentifier(spec.OutputPath), jobLogger, ct).ConfigureAwait(false);
        try
        {
            var templateSlide = await LoadTemplateSlideAsync(templateId, spec.TemplateSlideIndex, jobLogger, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Template slide #{spec.TemplateSlideIndex} not found in '{spec.TemplatePresentationPath}'.");
            var templateShapesByIdentifier = templateSlide.Shapes.ToDictionary(s => s.Identifier);

            var phase = initial.Phase;
            var currentIndex = initial.CurrentIndex;

            if (phase == JobPhase.CreatingOutput)
            {
                (phase, currentIndex) = (JobPhase.CreatingSlides, output.SlidesCount);
                await AdvancePhaseAsync(running, requestId, jobId, spec, phase, currentIndex, ct).ConfigureAwait(false);
            }

            if (phase == JobPhase.CreatingSlides)
            {
                await RunCreatingSlidesAsync(requestId, jobId, spec, output, templateSlide, running, dataRows, ct)
                    .ConfigureAwait(false);
                (phase, currentIndex) = (JobPhase.FillingText, 0);
                await AdvancePhaseAsync(running, requestId, jobId, spec, phase, currentIndex, ct).ConfigureAwait(false);
            }

            if (phase == JobPhase.FillingText)
            {
                await RunFillingTextAsync(requestId, jobId, spec, worksheet, headerToIndex, output, currentIndex,
                    dataRows, running, ct).ConfigureAwait(false);
                (phase, currentIndex) = (JobPhase.FillingImages, 0);
                await AdvancePhaseAsync(running, requestId, jobId, spec, phase, currentIndex, ct).ConfigureAwait(false);
            }

            if (phase == JobPhase.FillingImages)
            {
                var inspected = await InspectSourcesAsync(spec, worksheet, headerToIndex, dataRows, jobLogger, ct)
                    .ConfigureAwait(false);
                await RunFillingImagesAsync(requestId, jobId, spec, worksheet, headerToIndex,
                    templateShapesByIdentifier, output, currentIndex, dataRows, inspected, running, jobLogger, ct)
                    .ConfigureAwait(false);
            }

            jobLogger.LogInformation("Job complete: {OutputPath}", spec.OutputPath);
            return new JobRecord(requestId, jobId, Status.Complete, JobPhase.Done, dataRows.Count, spec,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            output.Dispose();
        }
    }

    private async Task AdvancePhaseAsync(
        RunningJob running, string requestId, int jobId, JobSpecification spec, JobPhase phase, int currentIndex,
        CancellationToken ct)
    {
        running.LastPhase = phase;
        running.LastIndex = currentIndex;
        var record = new JobRecord(requestId, jobId, Status.Running, phase, currentIndex, spec, DateTimeOffset.UtcNow);
        Persist(record);
        await jobsRepository.FlushAsync(ct).ConfigureAwait(false);
    }

    #region Phase A — output

    private async Task<IPresentation> OpenOutputAsync(PresentationIdentifier id, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Opening output: {OutputPath}", id.PresentationPath);
        var presentation = await presentationOpener.OpenPresentationAsync(id, ct).ConfigureAwait(false);
        if (presentation.IsWriteProtected) presentation.RemoveWriteProtection();
        return presentation;
    }

    private async Task<IPresentation> CreateOutputAsync(
        JobSpecification spec, PresentationIdentifier templateId, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Creating output: {OutputPath}", spec.OutputPath);
        var dir = Path.GetDirectoryName(spec.OutputPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.Copy(templateId.PresentationPath, spec.OutputPath, overwrite: true);

        var presentation = await OpenOutputAsync(new PresentationIdentifier(spec.OutputPath), logger, ct)
            .ConfigureAwait(false);
        for (var i = presentation.SlidesCount - 1; i >= 0; i--) presentation.RemoveSlideAt(i);
        return presentation;
    }

    private async Task<ISlide?> LoadTemplateSlideAsync(
        PresentationIdentifier templateId, int slideIndex, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Loading template slide: {Ppt} #{Index}", templateId.PresentationPath, slideIndex);
        using var template = await presentationOpener.OpenPresentationReadOnlyAsync(templateId, ct)
            .ConfigureAwait(false);
        var index = slideIndex - 1;
        return index < 0 || index >= template.SlidesCount ? null : template.Slides.ElementAt(index).Clone();
    }

    #endregion

    #region Phase B — slides

    private async Task RunCreatingSlidesAsync(
        string requestId, int jobId, JobSpecification spec, IPresentation output, ISlide templateSlide,
        RunningJob running, IReadOnlyList<int> dataRows, CancellationToken ct)
    {
        for (var i = output.SlidesCount; i < dataRows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await running.Gate.CheckpointAsync(ct).ConfigureAwait(false);

            output.AddSlide(templateSlide.Clone());
            output.Save();

            running.LastIndex = i + 1;
            Persist(new JobRecord(requestId, jobId, Status.Running, JobPhase.CreatingSlides, i + 1, spec,
                DateTimeOffset.UtcNow));
        }
    }

    #endregion

    #region Phase C — text

    private async Task RunFillingTextAsync(
        string requestId, int jobId, JobSpecification spec, IReadOnlyWorksheet worksheet,
        IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex, IPresentation output, int startIndex,
        IReadOnlyList<int> dataRows, RunningJob running, CancellationToken ct)
    {
        for (var i = startIndex; i < dataRows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await running.Gate.CheckpointAsync(ct).ConfigureAwait(false);

            var dataRow = dataRows[i];
            using var rowScope = LogContext.PushProperty("RowIndex", dataRow + 1);
            ReportRow(requestId, jobId, dataRow + 1, RowStatus.Processing);

            var rowValues = ReadRowValues(worksheet, headerToIndex, dataRow);
            var textValues = BuildRowTextValues(rowValues, spec.TextInstructions);
            var slide = output.Slides.ElementAt(i);
            foreach (var shape in slide.Shapes) textComposer.Compose(shape, textValues);
            output.Save();

            ReportRow(requestId, jobId, dataRow + 1, RowStatus.Done);
            running.LastIndex = i + 1;
            Persist(new JobRecord(requestId, jobId, Status.Running, JobPhase.FillingText, i + 1, spec,
                DateTimeOffset.UtcNow));
        }
    }

    /// <summary>
    ///     Reads a data row and builds a placeholder → value map. For each <see cref="TextInstruction" />
    ///     the first non-empty cell across its column list wins; all its placeholders map to that value.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> BuildRowTextValues(
        IReadOnlyDictionary<ColumnIdentifier, string> rowValues, IReadOnlyList<TextInstruction> instructions)
    {
        var result = new Dictionary<string, string>();
        foreach (var instruction in instructions)
        {
            var value = instruction.Columns
                .Select(col => rowValues.GetValueOrDefault(col))
                .FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";
            foreach (var placeholder in instruction.Placeholders) result[placeholder] = value;
        }

        return result;
    }

    #endregion

    #region Phase D — images

    private async Task<IReadOnlyDictionary<string, ContentInfo?>> InspectSourcesAsync(
        JobSpecification spec, IReadOnlyWorksheet worksheet, IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex,
        IReadOnlyList<int> dataRows, ILogger logger, CancellationToken ct)
    {
        var result = new Dictionary<string, ContentInfo?>();
        if (spec.ImageInstructions.Count == 0) return result;

        var sources = new HashSet<string>();
        foreach (var dataRow in dataRows)
        {
            var rowValues = ReadRowValues(worksheet, headerToIndex, dataRow);
            foreach (var instruction in spec.ImageInstructions)
            {
                var source = Utilities.GetSource(rowValues, instruction.Columns);
                if (string.IsNullOrWhiteSpace(source)) continue;
                sources.Add(source);
            }
        }

        foreach (var source in sources)
        {
            ct.ThrowIfCancellationRequested();
            result[source] = await InspectAsync(source, logger, ct).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<ContentInfo?> InspectAsync(string source, ILogger logger, CancellationToken ct)
    {
        logger.LogInformation("Resolving URL: {Source}", source);
        var urlStr = source.Contains("://") ? source : "https://" + source;
        if (!Uri.TryCreate(urlStr, UriKind.Absolute, out var parsedUri)) return null;

        using var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
        var info = await Utilities.ExecuteWithBackoffAsync(
            settingProvider.Current.Network.Retry.MaxRetries,
            TimeSpan.FromSeconds(settingProvider.Current.Network.Retry.MaxRetryDelay),
            () => cloudClient.InspectAsync(parsedUri, httpClient, ct), ct).ConfigureAwait(false);
        return info != null && info.IsImage() ? info : null;
    }

    private async Task RunFillingImagesAsync(
        string requestId, int jobId, JobSpecification spec, IReadOnlyWorksheet worksheet,
        IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex,
        IReadOnlyDictionary<ShapeIdentifier, IShape> templateShapesByIdentifier, IPresentation output,
        int startIndex, IReadOnlyList<int> dataRows, IReadOnlyDictionary<string, ContentInfo?> inspected,
        RunningJob running, ILogger logger, CancellationToken ct)
    {
        var tempDir = JobTempFolder(requestId, jobId);

        for (var i = startIndex; i < dataRows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await running.Gate.CheckpointAsync(ct).ConfigureAwait(false);

            var dataRow = dataRows[i];
            using var rowScope = LogContext.PushProperty("RowIndex", dataRow + 1);
            ReportRow(requestId, jobId, dataRow + 1, RowStatus.Processing);

            var rowValues = ReadRowValues(worksheet, headerToIndex, dataRow);
            var slide = output.Slides.ElementAt(i);
            var shapesByIdentifier = slide.Shapes.ToDictionary(s => s.Identifier);

            foreach (var instruction in spec.ImageInstructions)
            {
                var source = Utilities.GetSource(rowValues, instruction.Columns);
                foreach (var shapeIdentifier in instruction.Shapes)
                {
                    if (!templateShapesByIdentifier.TryGetValue(shapeIdentifier, out var templateShape)) continue;
                    if (!shapesByIdentifier.TryGetValue(shapeIdentifier, out var targetShape)) continue;

                    var targetSize = new Size(
                        System.Math.Max(1, (int)System.Math.Round(templateShape.Bounds.Width, MidpointRounding.AwayFromZero)),
                        System.Math.Max(1, (int)System.Math.Round(templateShape.Bounds.Height, MidpointRounding.AwayFromZero)));

                    inspected.TryGetValue(source, out var contentInfo);
                    var imageData = await ResolveShapeImageAsync(source, instruction, targetSize, spec, contentInfo,
                        tempDir, requestId, jobId, dataRow + 1, logger, ct).ConfigureAwait(false);
                    if (imageData != null) targetShape.ImageData = imageData;
                }
            }

            ReportRow(requestId, jobId, dataRow + 1, RowStatus.Processing, RowStage.SavingOutput);
            output.Save();
            ReportRow(requestId, jobId, dataRow + 1, RowStatus.Done);

            running.LastIndex = i + 1;
            Persist(new JobRecord(requestId, jobId, Status.Running, JobPhase.FillingImages, i + 1, spec,
                DateTimeOffset.UtcNow));
        }
    }

    private async Task<byte[]?> ResolveShapeImageAsync(
        string source, ImageInstruction instruction, Size targetSize, JobSpecification spec,
        ContentInfo? inspected, string tempDir, string requestId, int jobId, int rowIndex, ILogger logger,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(source))
        {
            if (File.Exists(source))
                return await EditImageFromPathAsync(source, targetSize, instruction.ImageEdits, rowIndex, logger, ct)
                    .ConfigureAwait(false);

            if (inspected != null)
            {
                var bytes = await EnsureDownloadedAsync(inspected, tempDir, requestId, jobId, rowIndex, logger, ct)
                    .ConfigureAwait(false);
                if (bytes != null)
                {
                    var edited = await EditImageFromBytesAsync(bytes, targetSize, instruction.ImageEdits, rowIndex,
                        logger, ct).ConfigureAwait(false);
                    if (edited != null) return edited;
                }
            }
        }

        if (instruction.FallbackImagePath is { } fallback && File.Exists(fallback))
            return await EditImageFromPathAsync(fallback, targetSize, instruction.ImageEdits, rowIndex, logger, ct)
                .ConfigureAwait(false);

        return null;
    }

    /// <summary>
    ///     Ensures the per-job download cache at <c>{tempDir}/{hash(resolvedUri)}{ext}</c> exists,
    ///     downloading it if not already present. Ponytail: no file-lock/wait-for-unlock dance anymore —
    ///     each job has its own temp folder (see <see cref="JobTempFolder" />) so no two jobs contend on
    ///     the same file; a within-job re-download race is harmless (same bytes, last write wins).
    /// </summary>
    private async Task<byte[]?> EnsureDownloadedAsync(
        ContentInfo inspected, string tempDir, string requestId, int jobId, int rowIndex, ILogger logger,
        CancellationToken ct)
    {
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, $"{inspected.Uri.AbsoluteUri.HashText()}{inspected.Extension}");
        if (File.Exists(path)) return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);

        if (Utilities.ExceedsMaxDownloadBytes(inspected, settingProvider.Current.Network.MaxDownloadBytes))
        {
            logger.LogWarning("Skipped download exceeding MaxDownloadBytes: {Uri}", inspected.Uri);
            return null;
        }

        ReportRow(requestId, jobId, rowIndex, RowStatus.Processing, RowStage.Downloading, inspected.Uri.ToString());
        using var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
        var bytes = await Utilities.ExecuteWithBackoffAsync(
            settingProvider.Current.Network.Retry.MaxRetries,
            TimeSpan.FromSeconds(settingProvider.Current.Network.Retry.MaxRetryDelay),
            () => cloudClient.DownloadAsync(inspected.Uri, httpClient, ct), ct).ConfigureAwait(false);
        if (bytes == null) return null;

        await File.WriteAllBytesAsync(path, bytes, ct).ConfigureAwait(false);
        return bytes;
    }

    private async Task<byte[]?> EditImageFromPathAsync(
        string path, Size targetSize, ImageEdits edits, int rowIndex, ILogger logger, CancellationToken ct)
    {
        using var image = imageLoader.Open(path);
        return await CropToPngAsync(image, targetSize, edits, path, rowIndex, logger, ct).ConfigureAwait(false);
    }

    private async Task<byte[]?> EditImageFromBytesAsync(
        byte[] data, Size targetSize, ImageEdits edits, int rowIndex, ILogger logger, CancellationToken ct)
    {
        using var image = imageLoader.Open(data);
        return await CropToPngAsync(image, targetSize, edits, "<in-memory>", rowIndex, logger, ct).ConfigureAwait(false);
    }

    private async Task<byte[]?> CropToPngAsync(
        IImage image, Size targetSize, ImageEdits edits, string sourceLabel, int rowIndex, ILogger logger,
        CancellationToken ct)
    {
        try
        {
            using var cropped = await smartCropper.CropAsync(image, targetSize, [.. edits.RoiOptions])
                .ConfigureAwait(false);
            return cropped?.ToPng();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Image edit failed: {Source}", sourceLabel);
            return null;
        }
    }

    #endregion

    #region Shared row helpers

    private static IReadOnlyDictionary<ColumnIdentifier, string> ReadRowValues(
        IReadOnlyWorksheet worksheet, IReadOnlyDictionary<ColumnIdentifier, int> headerToIndex, int dataRow)
    {
        var row = worksheet.GetRow(dataRow + 1); // row 1 = header => data row N = absolute row N+1
        return headerToIndex.ToDictionary(kv => kv.Key, kv => row[kv.Value]);
    }

    private void ReportRow(string requestId, int jobId, int rowIndex, RowStatus status,
        RowStage stage = RowStage.None, string? note = null) =>
        eventBus.Publish(new RowProgress
        {
            RequestId = requestId,
            JobId = jobId,
            RowIndex = rowIndex,
            Status = status,
            Stage = stage,
            Note = note,
            Timestamp = DateTimeOffset.UtcNow
        });

    #endregion
}

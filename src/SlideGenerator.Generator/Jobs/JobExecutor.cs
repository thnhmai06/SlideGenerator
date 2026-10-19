/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobExecutor.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using SlideGenerator.Cloud;
using SlideGenerator.Document.Presentations;
using SlideGenerator.Document.Presentations.Components;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Template;
using SlideGenerator.Document.Workbooks;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;
using SlideGenerator.Image.Cropping;
using SlideGenerator.Image.Loading;
using SlideGenerator.Recipe.Models;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Utilities;
using Math = System.Math;

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     What a <see cref="JobExecutor" /> sees of the workflow running it: resume flag, a per-workflow logger,
///     progress reporting for the job's own <see cref="JobSnapshot" />, and the cooperative pause checkpoint.
/// </summary>
internal interface IJobContext
{
    /// <summary><see langword="true" /> when the owning workflow is being resumed after a crash.</summary>
    bool IsResume { get; }

    /// <summary>The logger scoped to the owning workflow's log file, or <see langword="null" /> to use the default.</summary>
    ILogger? Logger { get; }

    /// <summary>Persists/publishes the job's latest state; <paramref name="durable" /> flushes it immediately.</summary>
    Task ReportAsync(JobSnapshot state, bool durable = false, CancellationToken ct = default);

    /// <summary>
    ///     Publishes the job's live progress to listeners WITHOUT persisting it (use ReportAsync for state that
    ///     must survive a crash).
    /// </summary>
    void Publish(JobSnapshot state);

    /// <summary>Blocks while the owning workflow is paused; returns immediately otherwise.</summary>
    Task CheckpointAsync(CancellationToken ct);
}

/// <summary>
///     The slide-generation job (create/open output → one pipeline per row) for one worksheet/presentation pair.
///     Knows nothing about concurrency, pause/resume beyond what
///     <see cref="IJobContext" /> hands it, or persistence beyond <see cref="IJobContext.ReportAsync" /> — the
///     owning <c>SlideGenerationWorkflow</c> handles those. Ported from the former <c>JobRunner.Phases.cs</c>.
/// </summary>
internal sealed class JobExecutor(
    IWorkbookOpener workbookOpener,
    IPresentationOpener presentationOpener,
    TextComposer textComposer,
    ISmartCropper smartCropper,
    IImageLoader imageLoader,
    ICloudClient cloudClient,
    IHttpClientFactory httpClientFactory,
    ISettingProvider settingProvider,
    IEventBus eventBus,
    ILogger<JobExecutor> logger)
{
    // The working file is saved after at most this many generated rows …
    private const int SaveEveryRows = 10;

    // … or after at most this long, whichever comes first.
    private static readonly TimeSpan SaveEveryInterval = TimeSpan.FromSeconds(5);

    /// <summary>Runs the job to completion, or until <paramref name="ct" /> is cancelled.</summary>
    public async Task<JobSnapshot> RunAsync(JobSnapshot initial, IJobContext context, CancellationToken ct)
    {
        var jobLogger = context.Logger ?? logger;
        var workflowId = initial.WorkflowId;
        var jobId = initial.JobId;
        var spec = initial.Specification;

        // The job works on its own .partial file and only moves it onto OutputPath once every row has been
        // generated, so a failed/cancelled run leaves the previous result in place. A fresh run clears a stale
        // working file.
        var partialPath = JobOutput.GetPartialPath(spec.OutputPath);
        if (!context.IsResume) PreflightCleanup.Run(partialPath, jobLogger);

        // The workbook is read once up front and released before any output/image work starts: every row the
        // pipeline needs is already in memory, so the source file is not held open for the whole job.
        List<int> dataRows;
        IReadOnlyList<IReadOnlyDictionary<ColumnIdentifier, string>> rows;
        using (var workbook = await workbookOpener.OpenWorkbookReadOnlyAsync(
                   new WorkbookIdentifier(spec.WorkbookPath), ct).ConfigureAwait(false))
        {
            var worksheet = workbook.GetWorksheet(spec.WorksheetName)
                            ?? throw new InvalidOperationException(
                                $"Sheet '{spec.WorksheetName}' not found in '{spec.WorkbookPath}'.");

            var headerToIndex = Utilities.FilterColumns(Utilities.BuildHeaderToIndexMap(worksheet), spec.UsedColumns);
            var dataCount = Math.Max(0, worksheet.RowCount - 1);
            dataRows = (spec.RowFilter?.GetIndices(dataCount) ?? Enumerable.Range(1, dataCount)).ToList();
            rows = Utilities.ReadRows(worksheet, headerToIndex, dataRows);
        }

        // Phase A — create/open output (always the working file).
        var templateId = new PresentationIdentifier(spec.TemplatePresentationPath);
        IPresentation output;
        // At CreatingOutput a leftover working file may be a template copy whose slides were not yet removed, so
        // it is recreated; from Generating on (or a legacy phase) it holds the job's progress and is reopened.
        if (initial.Phase != JobPhase.CreatingOutput && File.Exists(partialPath))
        {
            output = await OpenOutputAsync(new PresentationIdentifier(partialPath), jobLogger, ct)
                .ConfigureAwait(false);
        }
        else if (initial.Phase == JobPhase.CreatingOutput)
        {
            output = await CreateOutputAsync(partialPath, templateId, jobLogger, ct).ConfigureAwait(false);
        }
        else if (File.Exists(spec.OutputPath))
        {
            // A resumed job past CreatingOutput whose working file is gone, but the final file is there: the
            // previous attempt had already committed it and crashed before persisting Complete.
            jobLogger.LogInformation("Output already committed: {OutputPath}", spec.OutputPath);
            return new JobSnapshot(workflowId, jobId, JobStatus.Complete, JobPhase.Done, dataRows.Count, spec,
                DateTimeOffset.UtcNow, dataRows.Count);
        }
        else
        {
            throw new InvalidOperationException(
                $"Working file '{partialPath}' is missing; job {jobId} cannot resume.");
        }

        var outputDisposed = false;
        try
        {
            var (templatePresentation, templateSlide) = await LoadTemplateSlideAsync(
                    templateId, spec.TemplateSlideIndex, jobLogger, ct)
                .ConfigureAwait(false);
            // Kept alive for the rest of this method: Syncfusion's ISlide.Clone() doesn't fully detach from
            // its source presentation (still resolves layout/master through it), so AddSlide()-ing a clone
            // of templateSlide (see RunGeneratingAsync) throws NullReferenceException once the source
            // that produced it has been disposed.
            using var templatePresentationHandle = templatePresentation;
            if (templateSlide is null)
                throw new InvalidOperationException(
                    $"Template slide #{spec.TemplateSlideIndex} not found in '{spec.TemplatePresentationPath}'.");
            var templateShapesByIdentifier = templateSlide.Shapes.ToDictionary(s => s.Identifier);

            // CurrentIndex is the number of rows whose slide is already saved in the working file, so it is
            // exactly where the per-row pipeline continues. A legacy phase (CreatingSlides/FillingText/
            // FillingImages) starts over at row 0: the work is idempotent (existing slides are reused and
            // refilled).
            var startIndex = 0;
            if (initial.Phase == JobPhase.CreatingOutput)
            {
                await AdvancePhaseAsync(context, workflowId, jobId, spec, JobPhase.Generating, 0, dataRows.Count, ct)
                    .ConfigureAwait(false);
            }
            else if (initial.Phase == JobPhase.Generating)
            {
                startIndex = initial.CurrentIndex;
            }

            startIndex = Math.Clamp(startIndex, 0, dataRows.Count);

            var inspected = spec.ImageInstructions.Count > 0
                ? await InspectSourcesAsync(spec, rows, jobLogger, ct).ConfigureAwait(false)
                : new Dictionary<string, ContentInfo?>();

            await RunGeneratingAsync(workflowId, jobId, spec, rows, templateSlide, templateShapesByIdentifier,
                    output, startIndex, dataRows, inspected, context, jobLogger, ct)
                .ConfigureAwait(false);

            // Commit: release the working file, then rename it onto the final output path (same directory, so
            // this is an atomic replace). Only now does the new result replace the previous one.
            output.Save();
            output.Dispose();
            outputDisposed = true;
            File.Move(partialPath, spec.OutputPath, overwrite: true);

            jobLogger.LogInformation("Job complete: {OutputPath}", spec.OutputPath);
            return new JobSnapshot(workflowId, jobId, JobStatus.Complete, JobPhase.Done, dataRows.Count, spec,
                DateTimeOffset.UtcNow, dataRows.Count);
        }
        finally
        {
            if (!outputDisposed) output.Dispose();
        }
    }

    private static async Task AdvancePhaseAsync(
        IJobContext context, string workflowId, int jobId, JobSpecification spec, JobPhase phase,
        int currentIndex, int totalRows, CancellationToken ct)
    {
        var record = new JobSnapshot(workflowId, jobId, JobStatus.Running, phase, currentIndex, spec,
            DateTimeOffset.UtcNow, totalRows);
        await context.ReportAsync(record, true, ct).ConfigureAwait(false);
    }

    #region Phase A — output

    private async Task<IPresentation> OpenOutputAsync(PresentationIdentifier id, ILogger jobLogger,
        CancellationToken ct)
    {
        jobLogger.LogInformation("Opening output: {OutputPath}", id.PresentationPath);
        var presentation = await presentationOpener.OpenPresentationAsync(id, ct).ConfigureAwait(false);
        if (presentation.IsWriteProtected) presentation.RemoveWriteProtection();
        return presentation;
    }

    private async Task<IPresentation> CreateOutputAsync(
        string partialPath, PresentationIdentifier templateId, ILogger jobLogger, CancellationToken ct)
    {
        jobLogger.LogInformation("Creating output: {OutputPath}", partialPath);
        var dir = Path.GetDirectoryName(partialPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.Copy(templateId.PresentationPath, partialPath, true);

        var presentation = await OpenOutputAsync(new PresentationIdentifier(partialPath), jobLogger, ct)
            .ConfigureAwait(false);
        for (var i = presentation.SlidesCount - 1; i >= 0; i--) presentation.RemoveSlideAt(i);
        return presentation;
    }

    /// <summary>
    ///     Opens the template presentation and clones the requested slide out of it. Returns the opened
    ///     presentation alongside the clone — callers must keep it alive (not dispose of it) for as long as the
    ///     clone (or a further clone of it) may still be added to another presentation; see the caller in
    ///     <see cref="RunAsync" /> for why.
    /// </summary>
    private async Task<(IReadOnlyPresentation Template, ISlide? Slide)> LoadTemplateSlideAsync(
        PresentationIdentifier templateId, int slideIndex, ILogger jobLogger, CancellationToken ct)
    {
        jobLogger.LogInformation("Loading template slide: {Ppt} #{Index}", templateId.PresentationPath, slideIndex);
        var template = await presentationOpener.OpenPresentationReadOnlyAsync(templateId, ct)
            .ConfigureAwait(false);
        var index = slideIndex - 1;
        var slide = index < 0 || index >= template.SlidesCount ? null : template.Slides.ElementAt(index).Clone();
        return (template, slide);
    }

    #endregion

    #region Generating — one pipeline per row

    private async Task RunGeneratingAsync(
        string workflowId, int jobId, JobSpecification spec,
        IReadOnlyList<IReadOnlyDictionary<ColumnIdentifier, string>> rows, ISlide templateSlide,
        IReadOnlyDictionary<ShapeIdentifier, IShape> templateShapesByIdentifier, IPresentation output,
        int startIndex, IReadOnlyList<int> dataRows, IReadOnlyDictionary<string, ContentInfo?> inspected,
        IJobContext context, ILogger jobLogger, CancellationToken ct)
    {
        var tempDir = JobTempFolder.GetPath(workflowId, jobId);
        var lastSavedIndex = startIndex;
        var sinceSave = Stopwatch.StartNew();
        for (var i = startIndex; i < dataRows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await context.CheckpointAsync(ct).ConfigureAwait(false);

            var dataRow = dataRows[i];
            using var rowScope = LogContext.PushProperty("RowIndex", dataRow + 1);
            ReportRow(workflowId, jobId, dataRow + 1, RowStatus.Processing);

            // 1. Slide — one per row, in order. On a resume the working file may already hold it: the persisted
            // index only advances on a save, so the file can be ahead of it.
            if (output.SlidesCount < i)
                throw new InvalidOperationException(
                    $"Working file has {output.SlidesCount} slides but row {i} needs slide {i}.");
            if (output.SlidesCount == i) output.AddSlide(templateSlide.Clone());
            var slide = output.Slides.ElementAt(i);

            // 2. Text.
            var rowValues = rows[i];
            var textValues = BuildRowTextValues(rowValues, spec.TextInstructions);
            foreach (var shape in slide.Shapes) textComposer.Compose(shape, textValues);

            // 3. Images.
            var shapesByIdentifier = slide.Shapes.ToDictionary(s => s.Identifier);
            foreach (var instruction in spec.ImageInstructions)
            {
                var source = Utilities.GetSource(rowValues, instruction.Columns);
                foreach (var shapeIdentifier in instruction.Shapes)
                {
                    if (!templateShapesByIdentifier.TryGetValue(shapeIdentifier, out var templateShape)) continue;
                    if (!shapesByIdentifier.TryGetValue(shapeIdentifier, out var targetShape)) continue;

                    var targetSize = new Size(
                        Math.Max(1,
                            (int)Math.Round(templateShape.Bounds.Width, MidpointRounding.AwayFromZero)),
                        Math.Max(1,
                            (int)Math.Round(templateShape.Bounds.Height, MidpointRounding.AwayFromZero)));

                    inspected.TryGetValue(source, out var contentInfo);
                    var imageData = await ResolveShapeImageAsync(source, spec.AllowLocalPaths, instruction, targetSize,
                        contentInfo, tempDir, workflowId, jobId, dataRow + 1, jobLogger, ct).ConfigureAwait(false);
                    if (imageData != null) targetShape.ImageData = imageData;
                }
            }

            // Live progress only: the persisted index must never run ahead of the working file.
            ReportRow(workflowId, jobId, dataRow + 1, RowStatus.Done);
            context.Publish(new JobSnapshot(workflowId, jobId, JobStatus.Running, JobPhase.Generating, i + 1, spec,
                DateTimeOffset.UtcNow, dataRows.Count));

            var isLast = i == dataRows.Count - 1;
            if (isLast || i + 1 - lastSavedIndex >= SaveEveryRows || sinceSave.Elapsed >= SaveEveryInterval)
            {
                ReportRow(workflowId, jobId, dataRow + 1, RowStatus.Processing, RowStage.SavingOutput);
                output.Save();
                lastSavedIndex = i + 1;
                sinceSave.Restart();
                await context.ReportAsync(
                    new JobSnapshot(workflowId, jobId, JobStatus.Running, JobPhase.Generating, lastSavedIndex, spec,
                        DateTimeOffset.UtcNow, dataRows.Count),
                    false, ct).ConfigureAwait(false);
            }
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
                .Select(rowValues.GetValueOrDefault)
                .FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";
            foreach (var placeholder in instruction.Placeholders) result[placeholder] = value;
        }

        return result;
    }

    #endregion

    #region Image helpers

    private async Task<IReadOnlyDictionary<string, ContentInfo?>> InspectSourcesAsync(
        JobSpecification spec, IReadOnlyList<IReadOnlyDictionary<ColumnIdentifier, string>> rows,
        ILogger jobLogger, CancellationToken ct)
    {
        var result = new Dictionary<string, ContentInfo?>();
        if (spec.ImageInstructions.Count == 0) return result;

        // Only values the policy accepts as a network URL are collected here — a local path, free text or a
        // non-http scheme must never cause an InspectAsync call.
        var sources = new Dictionary<string, Uri>();
        foreach (var rowValues in rows)
        {
            foreach (var instruction in spec.ImageInstructions)
            {
                var source = Utilities.GetSource(rowValues, instruction.Columns);
                if (ImageSourcePolicy.Classify(source, spec.AllowLocalPaths, out var url) != ImageSourceKind.Url)
                    continue;
                sources[source] = url!;
            }
        }

        foreach (var (source, url) in sources)
        {
            ct.ThrowIfCancellationRequested();
            result[source] = await InspectAsync(url, jobLogger, ct).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<ContentInfo?> InspectAsync(Uri url, ILogger jobLogger, CancellationToken ct)
    {
        jobLogger.LogInformation("Resolving URL: {Source}", url);

        var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
        var info = await Utilities.ExecuteWithBackoffAsync(
            settingProvider.Current.Network.Retry.MaxRetries,
            TimeSpan.FromSeconds(settingProvider.Current.Network.Retry.MaxRetryDelay),
            () => cloudClient.InspectAsync(url, httpClient, ct), ct).ConfigureAwait(false);

        return info != null && info.IsImage() ? info : null;
    }

    private async Task<byte[]?> ResolveShapeImageAsync(
        string source, bool allowLocalPaths, ImageInstruction instruction, Size targetSize, ContentInfo? inspected,
        string tempDir, string workflowId, int jobId, int rowIndex, ILogger jobLogger, CancellationToken ct)
    {
        switch (ImageSourcePolicy.Classify(source, allowLocalPaths, out _))
        {
            case ImageSourceKind.LocalFile:
                return await EditImageFromPathAsync(source, targetSize, instruction.ImageEditInstruction, jobLogger)
                    .ConfigureAwait(false);

            case ImageSourceKind.Url when inspected != null:
            {
                var bytes = await EnsureDownloadedAsync(inspected, tempDir, workflowId, jobId, rowIndex, jobLogger, ct)
                    .ConfigureAwait(false);
                if (bytes != null)
                {
                    var edited = await EditImageFromBytesAsync(bytes, targetSize, instruction.ImageEditInstruction,
                        jobLogger).ConfigureAwait(false);
                    if (edited != null) return edited;
                }

                break;
            }
        }

        if (instruction.FallbackImagePath is { } fallback && File.Exists(fallback))
            return await EditImageFromPathAsync(fallback, targetSize, instruction.ImageEditInstruction, jobLogger)
                .ConfigureAwait(false);

        return null;
    }

    /// <summary>
    ///     Ensures the per-job download cache at <c>{tempDir}/{hash(resolvedUri)}{ext}</c> exists,
    ///     downloading it if not already present. Ponytail: no file-lock/wait-for-unlock dance — each job
    ///     has its own temp folder (see <see cref="JobTempFolder" />) so no two jobs contend on the same
    ///     file; a within-job re-download race is harmless (same bytes, last write wins).
    /// </summary>
    private async Task<byte[]?> EnsureDownloadedAsync(
        ContentInfo inspected, string tempDir, string workflowId, int jobId, int rowIndex, ILogger jobLogger,
        CancellationToken ct)
    {
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, $"{inspected.Uri.AbsoluteUri.HashText()}{inspected.Extension}");
        if (File.Exists(path)) return await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);

        if (Utilities.ExceedsMaxDownloadBytes(inspected, settingProvider.Current.Network.MaxDownloadBytes))
        {
            jobLogger.LogWarning("Skipped download exceeding MaxDownloadBytes: {Uri}", inspected.Uri);
            return null;
        }

        ReportRow(workflowId, jobId, rowIndex, RowStatus.Processing, RowStage.Downloading, inspected.Uri.ToString());
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
        string path, Size targetSize, ImageEditInstruction editInstruction, ILogger jobLogger)
    {
        using var image = imageLoader.Open(path);
        return await CropToPngAsync(image, targetSize, editInstruction, path, jobLogger).ConfigureAwait(false);
    }

    private async Task<byte[]?> EditImageFromBytesAsync(
        byte[] data, Size targetSize, ImageEditInstruction editInstruction, ILogger jobLogger)
    {
        using var image = imageLoader.Open(data);
        return await CropToPngAsync(image, targetSize, editInstruction, "<in-memory>", jobLogger)
            .ConfigureAwait(false);
    }

    private async Task<byte[]?> CropToPngAsync(
        IImage image, Size targetSize, ImageEditInstruction editInstruction, string sourceLabel, ILogger jobLogger)
    {
        try
        {
            using var cropped = await smartCropper.CropAsync(image, targetSize, [.. editInstruction.RoiOptions])
                .ConfigureAwait(false);
            return cropped?.ToPng();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            jobLogger.LogWarning(ex, "Image edit failed: {Source}", sourceLabel);
            return null;
        }
    }

    #endregion

    #region Shared row helpers

    private void ReportRow(string workflowId, int jobId, int rowIndex, RowStatus status,
        RowStage stage = RowStage.None, string? note = null)
    {
        eventBus.Publish(new RowProgress
        {
            WorkflowId = workflowId,
            JobId = jobId,
            RowIndex = rowIndex,
            Status = status,
            Stage = stage,
            Note = note,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    #endregion
}
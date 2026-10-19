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

    /// <summary>The image cache shared by every job of the owning workflow.</summary>
    ImageCache Images { get; }
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
    IEventBus eventBus,
    ILogger<JobExecutor> logger)
{
    // The working file is saved after at most this many generated rows …
    private const int SaveEveryRows = 10;

    // … or after at most this long, whichever comes first.
    private static readonly TimeSpan SaveEveryInterval = TimeSpan.FromSeconds(5);

    /// <summary>How many upcoming rows' image URLs to prefetch in the background.</summary>
    private const int PrefetchRows = 3;

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

            await RunGeneratingAsync(workflowId, jobId, spec, rows, templateSlide, templateShapesByIdentifier,
                    output, startIndex, dataRows, context, jobLogger, ct)
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
        int startIndex, IReadOnlyList<int> dataRows,
        IJobContext context, ILogger jobLogger, CancellationToken ct)
    {
        var lastSavedIndex = startIndex;
        var sinceSave = Stopwatch.StartNew();
        for (var i = startIndex; i < dataRows.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await context.CheckpointAsync(ct).ConfigureAwait(false);

            PrefetchAhead(rows, i, spec, context.Images);

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

                    var imageData = await ResolveShapeImageAsync(source, spec.AllowLocalPaths, instruction, targetSize,
                        context.Images, dataRow + 1, workflowId, jobId, jobLogger, ct).ConfigureAwait(false);
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

    private async Task<byte[]?> ResolveShapeImageAsync(
        string source, bool allowLocalPaths, ImageInstruction instruction, Size targetSize, ImageCache images,
        int rowIndex, string workflowId, int jobId, ILogger jobLogger, CancellationToken ct)
    {
        switch (ImageSourcePolicy.Classify(source, allowLocalPaths, out var url))
        {
            case ImageSourceKind.LocalFile:
                return await EditImageFromPathAsync(source, targetSize, instruction.ImageEditInstruction, jobLogger)
                    .ConfigureAwait(false);

            case ImageSourceKind.Url:
            {
                var path = await images.GetImagePathAsync(url!, u => ReportRow(workflowId, jobId, rowIndex,
                        RowStatus.Processing, RowStage.Downloading, u.ToString()), ct).ConfigureAwait(false);
                if (path != null)
                {
                    var edited = await EditImageFromPathAsync(path, targetSize, instruction.ImageEditInstruction,
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
    ///     Kicks off background prefetch for up to <see cref="PrefetchRows" /> upcoming rows' URLs. The cache
    ///     swallows every failure — this never throws.
    /// </summary>
    private static void PrefetchAhead(
        IReadOnlyList<IReadOnlyDictionary<ColumnIdentifier, string>> rows, int currentIndex,
        JobSpecification spec, ImageCache images)
    {
        if (spec.ImageInstructions.Count == 0) return;

        var last = Math.Min(currentIndex + PrefetchRows, rows.Count - 1);
        for (var r = currentIndex + 1; r <= last; r++)
        {
            var rowValues = rows[r];
            foreach (var instruction in spec.ImageInstructions)
            {
                if (ImageSourcePolicy.Classify(
                        Utilities.GetSource(rowValues, instruction.Columns),
                        spec.AllowLocalPaths,
                        out var url) == ImageSourceKind.Url)
                {
                    images.Prefetch(url!);
                }
            }
        }
    }

    private async Task<byte[]?> EditImageFromPathAsync(
        string path, Size targetSize, ImageEditInstruction editInstruction, ILogger jobLogger)
    {
        using var image = imageLoader.Open(path);
        return await CropToPngAsync(image, targetSize, editInstruction, path, jobLogger).ConfigureAwait(false);
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
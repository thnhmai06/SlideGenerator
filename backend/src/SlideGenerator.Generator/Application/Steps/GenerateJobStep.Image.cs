/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GenerateJobStep.Image.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Cloud.Models;
using SlideGenerator.Document.Abstractions.Slide;
using SlideGenerator.Document.Models.Sheet;
using SlideGenerator.Document.Models.Slide;
using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Enum;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Recipe.Domain.Models.Components;
using SlideGenerator.Settings.Rules;
using SlideGenerator.Utilities;

namespace SlideGenerator.Generator.Application.Steps;

public sealed partial class GenerateJobStep
{
    /// <summary>
    ///     A single image resolution request for one shape on one data row.
    /// </summary>
    /// <param name="Instruction">Image instruction carrying the fallback path and edit options.</param>
    /// <param name="Shape">Target shape identifier on the slide.</param>
    /// <param name="TargetSize">Pixel dimensions of the target shape at 96 DPI.</param>
    /// <param name="Source">Raw cell value (trimmed); <see cref="string.Empty" /> if the cell was empty.</param>
    private sealed record ImageRequest(
        ImageInstruction Instruction,
        ShapeIdentifier Shape,
        Size TargetSize,
        string Source);

    /// <summary>
    ///     Builds one <see cref="ImageRequest" /> per instruction/shape pair that targets an existing
    ///     template shape, resolving each shape's target pixel size from <paramref name="templateShapesByIdentifier" />.
    /// </summary>
    /// <param name="rowValues">Column-identifier-to-cell-value map for the data row.</param>
    /// <param name="instructions">Image instructions from the recipe map node.</param>
    /// <param name="templateShapesByIdentifier">Template shapes indexed by identifier (see <see cref="GenerateJobStep.RunAsync" />).</param>
    /// <returns>One request per (instruction, shape) pair whose shape exists on the template.</returns>
    private static List<ImageRequest> BuildRowImageRequests(
        IReadOnlyDictionary<ColumnIdentifier, string> rowValues,
        IReadOnlyList<ImageInstruction> instructions,
        IReadOnlyDictionary<ShapeIdentifier, IShape> templateShapesByIdentifier)
    {
        var imageRequests = new List<ImageRequest>();
        foreach (var instruction in instructions)
        {
            var source = Utilities.GetSource(rowValues, instruction.Columns);
            foreach (var shapeIdentifier in instruction.Shapes)
            {
                if (!templateShapesByIdentifier.TryGetValue(shapeIdentifier, out var templateShape)) continue;
                var targetSize = new Size(
                    System.Math.Max(1, (int)System.Math.Round(templateShape.Bounds.Width, MidpointRounding.AwayFromZero)),
                    System.Math.Max(1, (int)System.Math.Round(templateShape.Bounds.Height, MidpointRounding.AwayFromZero)));

                imageRequests.Add(new ImageRequest(instruction, shapeIdentifier, targetSize, source));
            }
        }

        return imageRequests;
    }

    /// <summary>
    ///     Resolves, for every pre-built image request of one data row, the target image bytes ready to
    ///     embed. Inspect results come from <see cref="_inspectedUrls" /> (populated once for the whole
    ///     job by <c>InspectUrlsStep</c>) — a source missing there is skipped, never re-inspected here.
    /// </summary>
    /// <param name="requests">Image requests for one data row.</param>
    /// <param name="allowLocalPaths">Whether local file paths are permitted as image sources.</param>
    /// <param name="rowIndex">1-based data row index this image belongs to, for row-scoped progress reporting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary keyed by (instruction, shape), value is the edited PNG bytes or <see langword="null" />.</returns>
    private async Task<Dictionary<(ImageInstruction, ShapeIdentifier), byte[]?>> CollectRowImagesAsync(
        IReadOnlyList<ImageRequest> requests,
        bool allowLocalPaths,
        int rowIndex,
        CancellationToken ct)
    {
        var result = new Dictionary<(ImageInstruction, ShapeIdentifier), byte[]?>();

        foreach (var req in requests)
        {
            ContentInfo? inspected = null;
            if (!string.IsNullOrWhiteSpace(req.Source))
                _inspectedUrls.TryGetValue(req.Source, out inspected);

            var data = await ResolveShapeImageAsync(
                    req.Source, req.Instruction, req.TargetSize, allowLocalPaths, inspected, rowIndex, ct)
                .ConfigureAwait(false);

            result[(req.Instruction, req.Shape)] = data;
        }

        return result;
    }

    /// <summary>
    ///     Resolves the edited image bytes for a single shape given a pre-extracted cell source and its
    ///     already-inspected result (looked up from <see cref="_inspectedUrls" />, never re-inspected here).
    ///     Priority: local file (if <paramref name="allowLocalPaths" /> is set) → resolved URL (permanent
    ///     temp-cache, shared across every row/job that resolves to the same URI) →
    ///     <see cref="ImageInstruction.FallbackImagePath" /> when the primary source is unavailable.
    /// </summary>
    private async Task<byte[]?> ResolveShapeImageAsync(
        string source,
        ImageInstruction instruction,
        Size targetSize,
        bool allowLocalPaths,
        ContentInfo? inspected,
        int rowIndex,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(source))
        {
            if (allowLocalPaths && File.Exists(source))
                return await EditImageFromPathAsync(source, targetSize, instruction.ImageEdits, rowIndex, ct)
                    .ConfigureAwait(false);

            if (inspected != null)
            {
                var (bytes, _) = await EnsureDownloadedAsync(inspected, rowIndex, ct).ConfigureAwait(false);
                var edited = bytes != null
                    ? await EditImageFromBytesAsync(bytes, targetSize, instruction.ImageEdits, rowIndex, ct)
                        .ConfigureAwait(false)
                    : null;
                if (edited != null) return edited;
            }
        }

        if (instruction.FallbackImagePath is { } fallback && File.Exists(fallback))
            return await EditImageFromPathAsync(fallback, targetSize, instruction.ImageEdits, rowIndex, ct)
                .ConfigureAwait(false);

        return null;
    }

    /// <summary>
    ///     Ensures the permanent temp-cached download for <paramref name="inspected" /> exists at
    ///     <c>{TempFolder}/{hash(resolvedUri)}{extension}</c>, downloading it if no other row/job has
    ///     already done so. The file is
    ///     never deleted after use — it stays cached for every future row/job resolving to the same URI,
    ///     until it naturally expires via the shared <see cref="ICache" />'s TTL eviction.
    ///     Concurrent downloader is serialized by an exclusive file creation (<see cref="FileMode.CreateNew" />):
    ///     whoever creates the file first becomes the writer; everyone else waits (via
    ///     <see cref="FileAccessHelper.WaitForFileChangeAsync" />) for it to appear/unlock, then reads it.
    ///     Readiness is always re-checked live on the filesystem, never cached.
    ///     A new download (not one already sitting in the temp-cache) is skipped entirely when
    ///     <paramref name="inspected" />'s known content length exceeds <c>Network.MaxDownloadBytes</c>
    ///     (0 = unlimited). Otherwise, the actual network call retries with truncated exponential backoff
    ///     up to <c>Network.Retry.MaxRetries</c> times (<see cref="Utilities.ExecuteWithBackoffAsync{T}" />).
    /// </summary>
    private async Task<(byte[]? Bytes, string TempPath)> EnsureDownloadedAsync(
        ContentInfo inspected, int rowIndex, CancellationToken ct)
    {
        var tempPath = IDownloadedFileCache.GetPath(inspected.Uri, inspected.Extension);
        Directory.CreateDirectory(NameAndPaths.TempFolder.RootPath);

        while (true)
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    return (await File.ReadAllBytesAsync(tempPath, ct).ConfigureAwait(false), tempPath);
                }
                catch (IOException ex) when (FileAccessHelper.IsFileLockedException(ex))
                {
                    await FileAccessHelper.WaitForFileChangeAsync(tempPath, ct).ConfigureAwait(false);
                    continue;
                }
            }

            if (Utilities.ExceedsMaxDownloadBytes(inspected, settingProvider.Current.Network.MaxDownloadBytes))
            {
                _logger.LogWarning(
                    "Skipped download exceeding MaxDownloadBytes: {Uri} ({Length} > {Max})",
                    inspected.Uri, inspected.Length, settingProvider.Current.Network.MaxDownloadBytes);
                return (null, tempPath);
            }

            FileStream fs;
            try
            {
                fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException)
            {
                await FileAccessHelper.WaitForFileChangeAsync(tempPath, ct).ConfigureAwait(false);
                continue;
            }

            try
            {
                await using (fs)
                {
                    _progress.ReportRow(rowIndex, RowStatus.Processing, RowStage.Downloading, inspected.Uri.ToString());
                    using var httpClient = httpClientFactory.CreateHttpClientWithSetting(settingProvider);
                    var bytes = await Utilities.ExecuteWithBackoffAsync(
                        settingProvider.Current.Network.Retry.MaxRetries,
                        TimeSpan.FromSeconds(settingProvider.Current.Network.Retry.MaxRetryDelay),
                        () => cloudClient.DownloadAsync(inspected.Uri, httpClient, ct),
                        ct).ConfigureAwait(false);
                    if (bytes == null)
                    {
                        if (File.Exists(tempPath)) File.Delete(tempPath);
                        return (null, tempPath);
                    }

                    await fs.WriteAsync(bytes, ct).ConfigureAwait(false);
                    return (bytes, tempPath);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Download failed: {Uri}", inspected.Uri);
                if (File.Exists(tempPath)) File.Delete(tempPath);
                return (null, tempPath);
            }
        }
    }

    /// <summary>Opens an image from disk and crops it.</summary>
    private async Task<byte[]?> EditImageFromPathAsync(
        string path, Size targetSize, ImageEdits edits, int rowIndex, CancellationToken ct)
    {
        using var image = imageLoader.Open(path);
        return await CropToPngAsync(image, targetSize, edits, path, rowIndex, ct).ConfigureAwait(false);
    }

    /// <summary>Opens an image from an in-memory buffer and crops it.</summary>
    private async Task<byte[]?> EditImageFromBytesAsync(
        byte[] data, Size targetSize, ImageEdits edits, int rowIndex, CancellationToken ct)
    {
        using var image = imageLoader.Open(data);
        return await CropToPngAsync(image, targetSize, edits, "<in-memory>", rowIndex, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Crops <paramref name="image" /> to <paramref name="targetSize" /> using smart-crop ROI logic
    ///     and returns the result as in-memory PNG bytes, or <see langword="null" /> if cropping returns
    ///     no result or an error occurs.
    /// </summary>
    private async Task<byte[]?> CropToPngAsync(
        IImage image, Size targetSize, ImageEdits edits, string sourceLabel, int rowIndex, CancellationToken ct)
    {
        try
        {
            _progress.ReportRow(rowIndex, RowStatus.Processing, RowStage.CroppingImage, sourceLabel);
            using var cropped = await smartCropper.CropAsync(image, targetSize, edits.RoiOptions.ToArray())
                .ConfigureAwait(false);
            return cropped?.ToPng();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image edit failed: {Source}", sourceLabel);
            return null;
        }
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ReplaceSlideData.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Fills a single slide with pre-calculated text and image replacements.
///     Avoids redundant file I/O by executing all replacements for a slide in one pass.
/// </summary>
public sealed class ReplaceSlideData(
    IGateLocker<GateType> gateLocker,
    ITextComposer textComposer,
    IPresentationProvider presentationProvider) : StepBodyAsync
{
    public SlideContext Task { get; init; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var ct = context.CancellationToken;
        var data = (GeneratingContext)context.Workflow.Data;
        var logger = data.LoggerFactory!.CreateLogger(nameof(ReplaceSlideData));

        if (Task.TextReplacements.Count == 0 && Task.ImageReplacements.Count == 0)
        {
            logger.LogDebug("No replacements found for row {RowIndex} in worksheet {SheetName}. Skipping.",
                Task.RowIndex,
                Task.SheetContext.WorksheetNode.Worksheet.SheetName);
            return ExecutionResult.Next();
        }

        logger.LogDebug("Data replacement start | Row: {RowIndex}, Worksheet: {SheetName}", Task.RowIndex,
            Task.SheetContext.WorksheetNode.Worksheet.SheetName);

        await gateLocker.AcquireAsync(GateType.EditPresentation, ct).ConfigureAwait(false);
        try
        {
            var wrapper = data.GetOrOpenOutput(presentationProvider, Task.SheetContext.OutputIdentifier);
            var slide = wrapper.Slides.ElementAt(Task.RowIndex - 1);

            foreach (var shape in slide.Shapes)
            {
                if (string.IsNullOrEmpty(shape.Name))
                    continue;

                logger.LogDebug("Processing shape '{ShapeName}' for row {RowIndex}", shape.Name,
                    Task.RowIndex);

                ApplyTextReplacements(logger, shape);
                await ApplyImageReplacementsAsync(data, logger, shape).ConfigureAwait(false);
            }

            wrapper.Save();

            logger.LogDebug("Data replacement completed | Row: {RowIndex}, Worksheet: {SheetName}",
                Task.RowIndex, Task.SheetContext.WorksheetNode.Worksheet.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            logger.LogError(ex, "FillSlideData failed for {SheetName} row {RowIndex}",
                Task.SheetContext.WorksheetNode.Worksheet.SheetName, Task.RowIndex);
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }

    private void ApplyTextReplacements(ILogger logger, IShape shape)
    {
        if (Task.TextReplacements.Count <= 0) return;
        logger.LogDebug("Applying text replacements to shape '{ShapeName}' (Count: {Count})", shape.Name,
            Task.TextReplacements.Count);
        textComposer.Compose(shape, Task.TextReplacements);
    }

    private async Task ApplyImageReplacementsAsync(GeneratingContext data, ILogger logger, IShape shape)
    {
        var matchingImageContext = Task.ImageReplacements.Values.FirstOrDefault(t =>
            t.ShapeName.Equals(shape.Name, StringComparison.OrdinalIgnoreCase));

        if (matchingImageContext != null)
        {
            var finalEditPath = matchingImageContext.EditPath + ".png";
            if (File.Exists(finalEditPath))
            {
                logger.LogDebug("Image shape replaced | Shape: {ShapeName}, Path: {Path}", shape.Name,
                    finalEditPath);

                shape.ImageData = await File.ReadAllBytesAsync(finalEditPath).ConfigureAwait(false);

                if (data.Request.EditAssetsPath == null)
                    try
                    {
                        File.Delete(finalEditPath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogTrace(ex, "Temp file cleanup skipped | Path: {Path}", finalEditPath);
                    }
            }
            else
            {
                logger.LogWarning("Edited image not found at '{Path}' for shape '{ShapeName}'", finalEditPath,
                    shape.Name);
            }
        }
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: ReplaceSlideData.cs
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
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Slide;
using SlideGenerator.Generating.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generating.Application.Steps;

/// <summary>
///     Fills a single slide with pre-calculated text and image replacements.
///     Avoids redundant file I/O by executing all replacements for a slide in one pass.
/// </summary>
public sealed class ReplaceSlideData(
    IGateLocker gateLocker,
    ITextComposer textComposer,
    IPresentationProvider presentationProvider) : StepBodyAsync
{
    public SlideContext Task { get; init; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        using var scope = data.Logger.BeginScope("ReplaceSlideData");

        if (Task.TextReplacements.Count == 0 && Task.ImageReplacements.Count == 0)
        {
            data.Logger.Debug("No replacements found for row {RowIndex} in sheet {SheetName}. Skipping.", Task.RowIndex,
                Task.SheetContext.Identifier.SheetName);
            return ExecutionResult.Next();
        }

        data.Logger.Information("Starting data replacement for row {RowIndex} in sheet {SheetName}", Task.RowIndex,
            Task.SheetContext.Identifier.SheetName);

        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            var wrapper = data.GetOrOpenOutput(presentationProvider, Task.SheetContext.OutputIdentifier);
            var slide = wrapper.Slides.ElementAt(Task.RowIndex - 1);

            foreach (var shape in slide.Shapes)
            {
                if (string.IsNullOrEmpty(shape.Name))
                    continue;

                data.Logger.Debug("Processing shape '{ShapeName}' for row {RowIndex}", shape.Name,
                    Task.RowIndex);

                ApplyTextReplacements(data, shape);
                await ApplyImageReplacementsAsync(data, shape).ConfigureAwait(false);
            }

            wrapper.Save();

            data.Logger.Information("Successfully replaced data for row {RowIndex} in sheet {SheetName}",
                Task.RowIndex, Task.SheetContext.Identifier.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            var path = $"{Task.SheetContext.Identifier.SheetName}_{Task.RowIndex}";
            using (data.Logger.BeginScope(path))
            {
                data.Logger.Error(ex, "FillSlideData failed");
            }
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return ExecutionResult.Next();
    }

    private void ApplyTextReplacements(GeneratingContext data, IShape shape)
    {
        if (Task.TextReplacements.Count > 0)
        {
            data.Logger.Debug("Applying text replacements to shape '{ShapeName}' (Count: {Count})", shape.Name,
                Task.TextReplacements.Count);
            textComposer.Compose(shape, Task.TextReplacements);
        }
    }

    private async Task ApplyImageReplacementsAsync(GeneratingContext data, IShape shape)
    {
        var matchingImageContext = Task.ImageReplacements.Values.FirstOrDefault(t =>
            t.ShapeName.Equals(shape.Name, StringComparison.OrdinalIgnoreCase));

        if (matchingImageContext != null)
        {
            var finalEditPath = matchingImageContext.EditPath + ".png";
            if (File.Exists(finalEditPath))
            {
                data.Logger.Information("Replacing image for shape '{ShapeName}' with '{Path}'", shape.Name,
                    finalEditPath);

                shape.ImageData = await File.ReadAllBytesAsync(finalEditPath).ConfigureAwait(false);

                if (data.Request.EditAssetsPath == null)
                    try
                    {
                        File.Delete(finalEditPath);
                    }
                    catch
                    {
                        /* ignore */
                    }
            }
            else
            {
                data.Logger.Warning("Edited image not found at '{Path}' for shape '{ShapeName}'", finalEditPath,
                    shape.Name);
            }
        }
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ExtractData.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Drawing;
using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Settings.Domain.Rules;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Consolidates data extraction into a single phase per worksheet.
///     Opens Excel and Presentations once, clones template slides,
///     and generates all SlideContexts and ImageContexts required for the worksheet.
/// </summary>
public sealed class ExtractData(
    IGateLocker<GateType> gateLocker,
    IWorkbookProvider workbookProvider,
    ITemplateEngine templateEngine)
    : StepBodyAsync
{
    public SheetContext Worksheet { get; init; } = null!;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var ct = context.CancellationToken;
        var logger = data.LoggerFactory!.CreateLogger(nameof(ExtractData));

        if (data.SlideContexts.Any(s => s.SheetContext.Identifier == Worksheet.Identifier))
        {
            logger.LogDebug("ExtractData already processed sheet {SheetName} — skipping (resume idempotency).",
                Worksheet.Identifier.SheetName);
            return ExecutionResult.Next();
        }

        logger.LogInformation("Starting data extraction for sheet {SheetName} in {BookPath}",
            Worksheet.Identifier.SheetName, Worksheet.Identifier.BookPath);

        try
        {
            var (rowCount, headerMap, sheet) = await ReadWorkbookMetadataAsync(data, logger, ct).ConfigureAwait(false);
            var shapeData = await CloneSlidesAndExtractShapeDataAsync(data, logger, rowCount, ct).ConfigureAwait(false);

            ConstructTasks(data, logger, rowCount, headerMap, sheet, shapeData);

            logger.LogInformation("Successfully extracted data and constructed tasks for sheet {SheetName}",
                Worksheet.Identifier.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            logger.LogError(ex, "ExtractData failed for sheet {SheetName}", Worksheet.Identifier.SheetName);
        }

        return ExecutionResult.Next();
    }

    private async Task<(int RowCount, Dictionary<string, int> HeaderMap, IReadOnlyWorksheet Sheet)>
        ReadWorkbookMetadataAsync(GeneratingContext data, ILogger logger, CancellationToken ct)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook, ct).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(workbookProvider, Worksheet.Identifier);

            var sheet = workbook.GetWorksheet(Worksheet.Identifier.SheetName)
                        ?? throw new KeyNotFoundException(
                            $"Sheet '{Worksheet.Identifier.SheetName}' not found in workbook '{Path.GetFileName(Worksheet.Identifier.BookPath)}'.");

            var rowCount = sheet.RowCount;
            var headers = sheet.GetRow(0);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
                if (!headerMap.ContainsKey(headers[i]))
                    headerMap[headers[i]] = i;

            logger.LogDebug("Found {RowCount} rows in sheet {SheetName}", rowCount,
                Worksheet.Identifier.SheetName);

            return (rowCount, headerMap, sheet);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
    }

    private async Task<Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)>>
        CloneSlidesAndExtractShapeDataAsync(GeneratingContext data, ILogger logger, int rowCount, CancellationToken ct)
    {
        var shapeData = new Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)>();

        await gateLocker.AcquireAsync(GateType.EditPresentation, ct).ConfigureAwait(false);
        try
        {
            if (data.OutputHandles.TryGetValue(Worksheet.OutputIdentifier, out var wrapper))
            {
                var templateSlide = wrapper.Slides.First();

                // Extract template shape info
                foreach (var shape in templateSlide.Shapes)
                {
                    if (string.IsNullOrEmpty(shape.Name)) continue;

                    var shapeId = new ShapeIdentifier(
                        Worksheet.TemplateSlide.PresentationPath,
                        Worksheet.TemplateSlide.SlideIndex,
                        shape.Name,
                        Worksheet.TemplateSlide.PresentationPassword);

                    var tags = templateEngine.ScanPlaceholders(shape.DisplayText);
                    var bounds = shape.Bounds;
                    shapeData[shapeId] = (shape.Name, tags, bounds);
                }

                logger.LogDebug("Extracted metadata for {Count} shapes from template slide", shapeData.Count);

                // Clone slides
                if (rowCount > 1)
                {
                    logger.LogDebug("Cloning {Count} additional slides for output", rowCount - 1);
                    for (var i = 1; i < rowCount; i++)
                        wrapper.CloneSlide(0);
                }

                wrapper.Save();
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Output handle not found for {Worksheet.OutputIdentifier.PresentationPath}");
            }
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }

        return shapeData;
    }

    private void ConstructTasks(
        GeneratingContext data,
        ILogger logger,
        int rowCount,
        Dictionary<string, int> headerMap,
        IReadOnlyWorksheet sheet,
        Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)> shapeData)
    {
        for (var rowIndex = 1; rowIndex <= rowCount; rowIndex++)
        {
            var slideTask = new SlideContext(Worksheet, rowIndex);
            var rowData = sheet.GetRow(rowIndex);

            MapTextReplacements(slideTask, rowData, headerMap, sheet, shapeData);
            MapImageReplacements(data, slideTask, rowData, headerMap, sheet, shapeData, rowIndex);

            if (slideTask.TextReplacements.Count <= 0 && slideTask.ImageReplacements.Count <= 0) continue;
            data.SlideContexts.Add(slideTask);
            logger.LogDebug("Mapped {TextCount} text and {ImageCount} image replacements for row {RowIndex}",
                slideTask.TextReplacements.Count, slideTask.ImageReplacements.Count, rowIndex);
        }
    }

    private void MapTextReplacements(
        SlideContext slideTask,
        IReadOnlyList<string> rowData,
        Dictionary<string, int> headerMap,
        IReadOnlyWorksheet sheet,
        Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)> shapeData)
    {
        foreach (var textInst in Worksheet.MapNode.TextInstructions)
        foreach (var column in textInst.Columns)
        {
            if (!column.SheetName.Equals(sheet.Name, StringComparison.OrdinalIgnoreCase) ||
                !column.BookPath.Equals(Worksheet.Identifier.BookPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!headerMap.TryGetValue(column.ColumnName, out var colIndex) || colIndex >= rowData.Count) continue;

            var val = rowData[colIndex];

            foreach (var placeholder in textInst.Placeholders)
            {
                var isUsed = shapeData.Values.Any(s => s.Tags.Contains(placeholder));
                if (isUsed) slideTask.TextReplacements[placeholder] = val;
            }
        }
    }

    private void MapImageReplacements(
        GeneratingContext data,
        SlideContext slideTask,
        IReadOnlyList<string> rowData,
        Dictionary<string, int> headerMap,
        IReadOnlyWorksheet sheet,
        Dictionary<ShapeIdentifier, (string ShapeName, HashSet<string> Tags, RectangleF Bounds)> shapeData,
        int rowIndex)
    {
        foreach (var imgInst in Worksheet.MapNode.ImageInstructions)
        foreach (var column in imgInst.Columns)
        {
            if (!column.SheetName.Equals(sheet.Name, StringComparison.OrdinalIgnoreCase) ||
                !column.BookPath.Equals(Worksheet.Identifier.BookPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!headerMap.TryGetValue(column.ColumnName, out var colIndex) || colIndex >= rowData.Count) continue;

            var rawUrl = rowData[colIndex];
            var sourceUrl = string.IsNullOrWhiteSpace(rawUrl) ? null : rawUrl.Trim();
            var downloadDir = NameAndPaths.AssetsFolder.GetDownloadDir(data.Request.DownloadAssetsPath,
                Worksheet.Identifier.BookPath, sheet.Name, column.ColumnName);
            var downloadPath = Path.Combine(downloadDir, rowIndex.ToString());

            foreach (var shapeId in imgInst.Shapes)
            {
                if (shapeId.PresentationPath != Worksheet.TemplateSlide.PresentationPath ||
                    shapeId.SlideIndex != Worksheet.TemplateSlide.SlideIndex) continue;
                if (!shapeData.TryGetValue(shapeId, out var sData)) continue;

                var editDir = NameAndPaths.AssetsFolder.GetEditDir(data.Request.EditAssetsPath,
                    Worksheet.Identifier.BookPath, sheet.Name, column.ColumnName);
                var editPath = Path.Combine(editDir, $"{rowIndex}_{sData.ShapeName}");

                var imageTask = new ImageContext(
                    Worksheet.Identifier, rowIndex, column.ColumnName,
                    sData.ShapeName, sourceUrl, downloadPath, editPath,
                    sData.Bounds.Width, sData.Bounds.Height,
                    imgInst.ImageEdits, imgInst.FallbackImagePath);

                data.ImageContexts.Add(imageTask);
                slideTask.ImageReplacements[shapeId] = imageTask;
            }
        }
    }
}
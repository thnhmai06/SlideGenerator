/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ValidateRequest.cs
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
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Recipe.Domain.Models.Graphs;
using SlideGenerator.Utilities;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

public sealed record ValidationItem(WorksheetNode Worksheet, MapNode Node);

/// <summary>
///     Validates a single worksheet and slide mapping, ensuring both exist and are accessible.
/// </summary>
public sealed class ValidateRequest(
    IWorkbookProvider workbookProvider,
    IPresentationProvider presentationProvider,
    IGateLocker<GateType> gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     The worksheet node and its associated map node to validate.
    /// </summary>
    public ValidationItem Item { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var ct = context.CancellationToken;
        var logger = data.LoggerFactory!.CreateLogger(nameof(ValidateRequest));
        var worksheetNode = Item.Worksheet;
        var mapNode = Item.Node;

        // Resolve the parent WorkbookNode to get the full file path.
        var workbookNode = data.RecipeGraph?.Nodes.OfType<WorkbookNode>()
            .FirstOrDefault(n => n.Id == worksheetNode.ParentId);
        if (workbookNode == null)
        {
            logger.LogError("Parent WorkbookNode '{ParentId}' not found for worksheet '{SheetName}'.",
                worksheetNode.ParentId, worksheetNode.Worksheet.SheetName);
            return ExecutionResult.Next();
        }

        // Resolve the target SlideNode and its parent PresentationNode via edges.
        var slideNodeId = data.RecipeGraph!.Edges
            .Where(e => e.FromId == mapNode.Id)
            .Select(e => e.ToId)
            .FirstOrDefault();
        var slideNode = slideNodeId != null
            ? data.RecipeGraph.Nodes.OfType<SlideNode>().FirstOrDefault(n => n.Id == slideNodeId)
            : null;
        var presentationNode = slideNode != null
            ? data.RecipeGraph.Nodes.OfType<PresentationNode>().FirstOrDefault(n => n.Id == slideNode.ParentId)
            : null;

        if (slideNode == null || presentationNode == null)
        {
            logger.LogError("No valid SlideNode target found for MapNode '{MapId}'.", mapNode.Id);
            return ExecutionResult.Next();
        }

        logger.LogInformation("Validating worksheet '{SheetName}' → slide {SlideIndex}",
            worksheetNode.Worksheet.SheetName, slideNode.Slide.SlideIndex);

        try
        {
            await ValidateWorksheetAsync(data, logger, workbookNode, worksheetNode, ct).ConfigureAwait(false);
            await ValidatePresentationAndMapOutputAsync(
                    data, logger, workbookNode, worksheetNode, mapNode, presentationNode, slideNode, ct)
                .ConfigureAwait(false);

            logger.LogInformation("Validation successful for worksheet '{SheetName}'",
                worksheetNode.Worksheet.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            logger.LogError(ex, "Validation failed for '{BookPath}/{SheetName}'",
                workbookNode.Workbook.BookPath, worksheetNode.Worksheet.SheetName);
        }

        return ExecutionResult.Next();
    }

    private async Task ValidateWorksheetAsync(
        GeneratingContext data, ILogger logger,
        WorkbookNode workbookNode, WorksheetNode worksheetNode, CancellationToken ct)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook, ct).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(workbookProvider, workbookNode.Workbook);

            var worksheet = workbook.GetWorksheet(worksheetNode.Worksheet.SheetName);
            if (worksheet == null)
                throw new ArgumentException(
                    $"Worksheet '{worksheetNode.Worksheet.SheetName}' not found in workbook " +
                    $"'{Path.GetFileName(workbookNode.Workbook.BookPath)}'.");

            logger.LogDebug("Verified workbook '{BookName}' contains worksheet '{SheetName}'",
                Path.GetFileName(workbookNode.Workbook.BookPath), worksheetNode.Worksheet.SheetName);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
    }

    private async Task ValidatePresentationAndMapOutputAsync(
        GeneratingContext data, ILogger logger,
        WorkbookNode workbookNode, WorksheetNode worksheetNode,
        MapNode mapNode, PresentationNode presentationNode, SlideNode slideNode,
        CancellationToken ct)
    {
        await gateLocker.AcquireAsync(GateType.ReadPresentation, ct).ConfigureAwait(false);
        try
        {
            var template = data.GetOrOpenPresentation(presentationProvider, presentationNode.Presentation);

            if (slideNode.Slide.SlideIndex > template.SlidesCount)
                throw new ArgumentException(
                    $"Slide index {slideNode.Slide.SlideIndex} is out of range for " +
                    $"'{Path.GetFileName(presentationNode.Presentation.PresentationPath)}' (Count: {template.SlidesCount}).");

            logger.LogDebug("Verified presentation '{PresentationName}' contains slide index {Index}",
                Path.GetFileName(presentationNode.Presentation.PresentationPath), slideNode.Slide.SlideIndex);

            var bookName = Path.GetFileNameWithoutExtension(workbookNode.Workbook.BookPath);
            var outputFileName =
                $"{Naming.SanitizeFileName(worksheetNode.Worksheet.SheetName)}{data.Request.OutputType.ToExtension()}";
            var outputPath = Path.Combine(data.Request.SaveFolder, bookName, outputFileName);
            var outputIdentifier = new PresentationIdentifier(outputPath);

            data.ValidWorksheets.TryAdd(
                worksheetNode.Id,
                new SheetContext(workbookNode.Workbook, worksheetNode, slideNode, mapNode,
                    presentationNode.Presentation, outputIdentifier));

            logger.LogDebug("Output path mapped to: '{Path}'", outputPath);
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }
    }
}
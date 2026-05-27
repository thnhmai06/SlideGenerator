/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ValidateRequest.cs
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
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Document.Domain.Models.Slide;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Summarization.Domain.Models.Recipes;
using SlideGenerator.Utilities.Helper;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

public sealed record ValidationItem(SheetIdentifier Sheet, MapNode Node);

/// <summary>
///     Validates a single sheet and slide mapping, ensuring both exist and are accessible.
/// </summary>
public sealed class ValidateRequest(
    IWorkbookProvider workbookProvider,
    IPresentationProvider presentationProvider,
    IGateLocker gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     The sheet and its associated map node to validate.
    /// </summary>
    public ValidationItem Item { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;

        var ct = context.CancellationToken;
        using var scope = data.Logger.BeginScope("ValidateRequest");
        var sheet = Item.Sheet;
        var node = Item.Node;
        var slide = node.Slide;

        data.Logger.Information("Validating request for sheet {SheetName} and slide index {SlideIndex}",
            sheet.SheetName, slide.SlideIndex);

        try
        {
            await ValidateWorksheetAsync(data, sheet, ct).ConfigureAwait(false);
            await ValidatePresentationAndMapOutputAsync(data, sheet, node, slide, ct).ConfigureAwait(false);

            data.Logger.Information("Validation successful for sheet {SheetName}", sheet.SheetName);
        }
        catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                       and not IndexOutOfRangeException)
        {
            var path = $"{sheet.BookPath}_{sheet.SheetName}";
            using (data.Logger.BeginScope(path))
            {
                data.Logger.Error(ex, "Validation failed");
            }
        }

        return ExecutionResult.Next();
    }

    private async Task ValidateWorksheetAsync(GeneratingContext data, SheetIdentifier sheet, CancellationToken ct)
    {
        await gateLocker.AcquireAsync(GateType.ReadWorkbook, ct).ConfigureAwait(false);
        try
        {
            var workbook = data.GetOrOpenWorkbook(workbookProvider, sheet);

            var worksheet = workbook.GetWorksheet(sheet.SheetName);
            if (worksheet == null)
                throw new ArgumentException(
                    $"Sheet '{sheet.SheetName}' not found in workbook '{Path.GetFileName(sheet.BookPath)}'.");

            data.Logger.Debug("Verified workbook '{BookName}' contains sheet '{SheetName}'",
                Path.GetFileName(sheet.BookPath), sheet.SheetName);
        }
        finally
        {
            gateLocker.Release(GateType.ReadWorkbook);
        }
    }

    private async Task ValidatePresentationAndMapOutputAsync(GeneratingContext data, SheetIdentifier sheet,
        MapNode node,
        SlideIdentifier slide, CancellationToken ct)
    {
        await gateLocker.AcquireAsync(GateType.ReadPresentation, ct).ConfigureAwait(false);
        try
        {
            var template = data.GetOrOpenPresentation(presentationProvider, slide);

            if (slide.SlideIndex <= 0 || slide.SlideIndex > template.SlidesCount)
                throw new ArgumentException(
                    $"Slide index {slide.SlideIndex} is out of range for '{Path.GetFileName(slide.PresentationPath)}' (Count: {template.SlidesCount}).");

            data.Logger.Debug("Verified presentation '{PresentationName}' contains slide index {Index}",
                Path.GetFileName(slide.PresentationPath), slide.SlideIndex);

            // Successful validation: Prepare output mapping
            var bookName = Path.GetFileNameWithoutExtension(sheet.BookPath);
            var outputFileName =
                $"{Normalization.SanitizeFileName(sheet.SheetName)}{data.Request.OutputType.ToExtension()}";
            var outputPath = Path.Combine(data.Request.SaveFolder, bookName, outputFileName);
            var outputIdentifier = new PresentationIdentifier(outputPath);

            data.ValidWorksheets.TryAdd(sheet, new SheetContext(sheet, slide, node, outputIdentifier));

            data.Logger.Debug("Output path mapped to: '{Path}'", outputPath);
        }
        finally
        {
            gateLocker.Release(GateType.ReadPresentation);
        }
    }
}
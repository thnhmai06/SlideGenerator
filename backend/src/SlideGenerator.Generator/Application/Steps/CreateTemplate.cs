/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: CreateTemplate.cs
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

using Microsoft.Extensions.Logging;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models;
using SlideGenerator.Generator.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Creates the output presentation file by copying the template and isolating
///     the single template slide to be used for cloning.
/// </summary>
public sealed class CreateTemplate(
    IGateLocker<GateType> gateLocker,
    IPresentationProvider presentationProvider) : StepBodyAsync
{
    /// <summary>
    ///     The validation item containing sheet and node info.
    /// </summary>
    public ValidationItem Item { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var ct = context.CancellationToken;
        var data = (GeneratingContext)context.Workflow.Data;
        var logger = data.LoggerFactory!.CreateLogger(nameof(CreateTemplate));

        if (!data.ValidWorksheets.TryGetValue(Item.Sheet, out var worksheet))
        {
            logger.LogError(
                new KeyNotFoundException($"Worksheet '{Item.Sheet.SheetName}' was not found in validated results."),
                "CreateTemplate validation failed for sheet {SheetName}", Item.Sheet.SheetName);
        }
        else
        {
            try
            {
                logger.LogInformation("Initializing output template for sheet {SheetName}",
                    worksheet.Identifier.SheetName);

                await CreateTemplateFileAsync(data, logger, worksheet, ct).ConfigureAwait(false);

                logger.LogInformation("Successfully initialized output presentation at '{Path}'",
                    worksheet.OutputIdentifier.PresentationPath);
            }
            catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                           and not IndexOutOfRangeException)
            {
                logger.LogError(ex, "CreateTemplate execution failed for sheet {SheetName}",
                    worksheet.Identifier.SheetName);
            }
        }

        return ExecutionResult.Next();
    }

    private async Task CreateTemplateFileAsync(
        GeneratingContext data, ILogger logger, SheetContext validatedSheet, CancellationToken ct)
    {
        // 1. Ensure the output directory exists. Idempotent: workspace-wide cleanup is the job of
        //    PreflightCleanup; deleting here would clobber sibling sheets' outputs.
        var outputDir = Path.GetDirectoryName(validatedSheet.OutputIdentifier.PresentationPath);
        if (outputDir != null)
        {
            Directory.CreateDirectory(outputDir);
            logger.LogDebug("Ensured output directory exists: '{Path}'", outputDir);
        }

        // 2. Copy the template to the output path (overwrite if it exists)
        logger.LogDebug("Copying template from '{Source}' to '{Destination}'",
            validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputIdentifier.PresentationPath);
        File.Copy(validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputIdentifier.PresentationPath,
            true);

        // 3. Isolate a template slide (Delete all other slides)
        await gateLocker.AcquireAsync(GateType.EditPresentation, ct).ConfigureAwait(false);
        try
        {
            logger.LogDebug("Isolating slide at index {Index} in output presentation",
                validatedSheet.TemplateSlide.SlideIndex);

            var presentation = presentationProvider.OpenPresentation(validatedSheet.OutputIdentifier);
            data.OutputHandles.TryAdd(validatedSheet.OutputIdentifier, presentation);

            // Remove all slides except the one at targetIndex.
            // Iterate backwards to safely remove by index.
            var templateIndex = validatedSheet.TemplateSlide.SlideIndex - 1;
            var originalCount = presentation.SlidesCount;
            for (var i = originalCount - 1; i >= 0; i--)
                if (i != templateIndex)
                    presentation.RemoveSlideAt(i);

            logger.LogDebug("Removed {Count} unrelated slides from the template copy", originalCount - 1);

            presentation.RemoveEncryption();
            presentation.RemoveWriteProtection();
            presentation.Save();
        }
        finally
        {
            gateLocker.Release(GateType.EditPresentation);
        }
    }
}

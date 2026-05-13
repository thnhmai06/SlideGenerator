/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
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
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Generating.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generating.Application.Steps;

/// <summary>
///     Creates the output presentation file by copying the template and isolating
///     the single template slide to be used for cloning.
/// </summary>
public sealed class CreateTemplate(
    IGateLocker gateLocker, 
    IPresentationProvider presentationProvider) : StepBodyAsync
{
    /// <summary>
    ///     The validation item containing sheet and node info.
    /// </summary>
    public ValidationItem Item { get; init; } = null!;

    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        using var scope = data.Logger.BeginScope("CreateTemplate");

        if (!data.ValidWorksheets.TryGetValue(Item.Sheet, out var worksheet))
        {
            var ex = new KeyNotFoundException(
                $"Worksheet '{Item.Sheet.SheetName}' was not found in validated results.");
            using (data.Logger.BeginScope(Item.Sheet.SheetName))
                data.Logger.Error(ex, "CreateTemplate validation failed");
        }
        else
        {
            try
            {
                data.Logger.Information("Initializing output template for sheet {SheetName}",
                    worksheet.Identifier.SheetName);

                await CreateTemplateFileAsync(data, worksheet).ConfigureAwait(false);

                data.Logger.Information("Successfully initialized output presentation at '{Path}'",
                    worksheet.OutputIdentifier.PresentationPath);
            }
            catch (Exception ex) when (ex is not NullReferenceException and not InvalidCastException
                                           and not IndexOutOfRangeException)
            {
                using (data.Logger.BeginScope(worksheet.Identifier.SheetName))
                    data.Logger.Error(ex, "CreateTemplate execution failed");
            }
        }

        return ExecutionResult.Next();
    }

    private async Task CreateTemplateFileAsync(GeneratingContext data, SheetContext validatedSheet)
    {
        // 1. Ensure output directory exists
        var outputDir = Path.GetDirectoryName(validatedSheet.OutputIdentifier.PresentationPath);
        if (outputDir != null)
        {
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);
            data.Logger.Debug("Created output directory: '{Path}'", outputDir);
        }

        // 2. Copy the template to the output path (overwrite if it exists)
        data.Logger.Debug("Copying template from '{Source}' to '{Destination}'",
            validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputIdentifier.PresentationPath);
        File.Copy(validatedSheet.TemplateSlide.PresentationPath, validatedSheet.OutputIdentifier.PresentationPath);

        // 3. Isolate template slide (Delete all other slides)
        await gateLocker.AcquireAsync(GateType.EditPresentation).ConfigureAwait(false);
        try
        {
            data.Logger.Debug("Isolating slide at index {Index} in output presentation",
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

            data.Logger.Debug("Removed {Count} unrelated slides from the template copy", originalCount - 1);

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







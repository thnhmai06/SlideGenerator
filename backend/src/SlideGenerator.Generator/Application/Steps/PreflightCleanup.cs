/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: PreflightCleanup.cs
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
using SlideGenerator.Generator.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Runs once at the start of the workflow to wipe every output directory associated with the
///     current recipe. Replaces the per-sheet <c>Directory.Delete</c> previously embedded in
///     <c>CreateTemplate</c>, which caused multi-sheet workbooks to destroy each other's output.
/// </summary>
public sealed class PreflightCleanup : StepBody
{
    /// <inheritdoc />
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        using var scope = data.Logger!.BeginScope("PreflightCleanup");

        if (data.RecipeSummary == null)
        {
            data.Logger.LogDebug("No recipe summary present; nothing to clean.");
            return ExecutionResult.Next();
        }

        // Collect distinct workbook-bound output roots: SaveFolder/<bookName>/
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in data.RecipeSummary.Nodes)
        foreach (var sheet in node.Sheets)
        {
            var bookName = Path.GetFileNameWithoutExtension(sheet.BookPath);
            if (string.IsNullOrEmpty(bookName)) continue;
            var dir = Path.Combine(data.Request.SaveFolder, bookName);
            if (!seen.Add(dir)) continue;

            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                    data.Logger.LogDebug("Removed existing output directory '{Dir}'", dir);
                }

                Directory.CreateDirectory(dir);
            }
            catch (Exception ex) when (ex is not NullReferenceException
                                           and not InvalidCastException
                                           and not IndexOutOfRangeException)
            {
                data.Logger.LogWarning(ex, "Failed to clean output directory '{Dir}'", dir);
            }
        }

        return ExecutionResult.Next();
    }
}
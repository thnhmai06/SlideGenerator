/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: LoadRecipeSummary.cs
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
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Summarization.Application.Abstractions;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Loads the recipe from the repository and builds the transient <see cref="GeneratingContext.RecipeSummary" />
///     and the persisted <see cref="GeneratingContext.ValidationItems" /> snapshot used by Phase A.
///     Running this as the first workflow step ensures both first-run and resume scenarios have a fresh recipe,
///     while the guard on <c>IRecipeRepository</c> prevents the recipe from changing mid-workflow.
/// </summary>
public sealed class LoadRecipeSummary(
    IRecipeRepository recipeRepository,
    ISummarizationService summarizationService) : StepBodyAsync
{
    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var ct = context.CancellationToken;
        using var scope = data.Logger!.BeginScope("LoadRecipeSummary");

        var entry = await recipeRepository.GetByIdAsync(data.Request.RecipeId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException(
                        $"Recipe {data.Request.RecipeId} not found — it may have been deleted.");

        data.RecipeSummary = summarizationService.SummarizeRecipe(entry.Recipe ?? string.Empty);

        data.ValidationItems = data.RecipeSummary.Nodes
            .SelectMany(node => node.Sheets.Select(sheet => new ValidationItem(sheet, node)))
            .ToList();

        data.Logger.LogInformation(
            "Loaded recipe {RecipeId} with {NodeCount} node(s) and {ItemCount} validation item(s).",
            data.Request.RecipeId, data.RecipeSummary.Nodes.Count, data.ValidationItems.Count);

        return ExecutionResult.Next();
    }
}
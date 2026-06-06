/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: LoadRecipeSummary.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Recipe.Application.Abstractions;
using SlideGenerator.Recipe.Domain.Models.Graphs;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Generator.Application.Steps;

/// <summary>
///     Loads the recipe graph from the repository and builds the transient <see cref="GeneratingContext.RecipeGraph" />
///     and the persisted <see cref="GeneratingContext.ValidationItems" /> snapshot used by Phase A.
///     Running this as the first workflow step ensures both first-run and resume scenarios have a fresh recipe,
///     while the guard on <c>IRecipeRepository</c> prevents the recipe from changing mid-workflow.
/// </summary>
public sealed class LoadRecipeSummary(IRecipeRepository recipeRepository) : StepBodyAsync
{
    /// <inheritdoc />
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var data = (GeneratingContext)context.Workflow.Data;
        var ct = context.CancellationToken;
        var logger = data.LoggerFactory!.CreateLogger(nameof(LoadRecipeSummary));

        data.RecipeGraph = (await recipeRepository.GetAsync(data.Request.RecipeId, ct).ConfigureAwait(false)).Graph;

        if (data.RecipeGraph is { } graph)
            data.ValidationItems = graph.Nodes.OfType<MapNode>()
                .SelectMany(mapNode =>
                {
                    var incomingIds = graph.Edges
                        .Where(e => e.ToId == mapNode.Id)
                        .Select(e => e.FromId)
                        .ToHashSet();
                    return graph.Nodes.OfType<WorksheetNode>()
                        .Where(ws => incomingIds.Contains(ws.Id))
                        .Select(ws => new ValidationItem(ws, mapNode));
                })
                .ToList();

        logger.LogInformation(
            "Loaded recipe {RecipeId} with {ItemCount} validation item(s).",
            data.Request.RecipeId, data.ValidationItems.Count);

        return ExecutionResult.Next();
    }
}
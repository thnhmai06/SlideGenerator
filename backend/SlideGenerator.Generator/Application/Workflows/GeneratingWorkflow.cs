/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingWorkflow.cs
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

using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Domain.Models.Contexts;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Primitives;

namespace SlideGenerator.Generator.Application.Workflows;

/// <summary>Identifies the execution phase of the generating workflow.</summary>
public enum GeneratingPhase
{
    PhaseA,
    PhaseB,
    PhaseC
}

/// <summary>
///     Orchestrates the slide generation process strictly using WorkflowCore iterators.
///     Phase A: Validation & Template Setup
///     Phase B: Preparation, Metadata Extraction, Resource Fetching
///     Phase C: Assembly & Finalization
/// </summary>
public sealed class GeneratingWorkflow : IWorkflow<GeneratingContext>
{
    /// <inheritdoc />
    public string Id => nameof(GeneratingWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public void Build(IWorkflowBuilder<GeneratingContext> builder)
    {
        PhaseC(PhaseB(PhaseA(builder)));
    }

    /// <summary>Phase A: Validation &amp; Template Setup</summary>
    private static IStepBuilder<GeneratingContext, InlineStepBody> PhaseA(IWorkflowBuilder<GeneratingContext> builder)
    {
        return builder
            .ForEach(data =>
                data.RecipeSummary!.Nodes.SelectMany(node =>
                    node.Sheets.Select(sheet => new ValidationItem(sheet, node))))
            .Do(x => x
                .StartWith<ValidateRequest>()
                .Input(step => step.Item, (data, context) => (ValidationItem)context.Item)
                .Then<CreateTemplate>()
                .Input(step => step.Item, (data, context) => (ValidationItem)context.Item))
            .Then(_ => ExecutionResult.Next());
    }

    /// <summary>Phase B: Resource Preparation (Extract Data, Download &amp; Edit Images)</summary>
    private static IStepBuilder<GeneratingContext, InlineStepBody> PhaseB(
        IStepBuilder<GeneratingContext, InlineStepBody> prev)
    {
        return prev
            .ForEach(data => data.ValidWorksheets.Values)
            .Do(x => x
                .StartWith<ExtractData>()
                .Input(step => step.Worksheet, (data, context) => context.Item as SheetContext))
            .Then(_ => ExecutionResult.Next())
            .ForEach(data => data.ImageContexts)
            .Do(x => x
                .StartWith<DownloadImage>()
                .Input(step => step.Task, (data, context) => context.Item as ImageContext)
                .Then<EditImage>()
                .Input(step => step.Task, (data, context) => context.Item as ImageContext))
            .Then(_ => ExecutionResult.Next());
    }

    /// <summary>Phase C: Assembly &amp; Finalization</summary>
    private static void PhaseC(IStepBuilder<GeneratingContext, InlineStepBody> prev)
    {
        prev
            .ForEach(data => data.SlideContexts)
            .Do(x => x
                .StartWith<ReplaceSlideData>()
                .Input(step => step.Task, (data, context) => context.Item as SlideContext))
            .Then(_ => ExecutionResult.Next())
            .Then<CloseAllHandles>();
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingWorkflow.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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
        var load = builder.StartWith<LoadRecipeSummary>();
        var preflight = load.Then<PreflightCleanup>();
        PhaseC(PhaseB(PhaseA(preflight)));
    }

    /// <summary>Phase A: Validation &amp; Template Setup</summary>
    private static IStepBuilder<GeneratingContext, InlineStepBody> PhaseA(
        IStepBuilder<GeneratingContext, PreflightCleanup> preflight)
    {
        return preflight
            .ForEach(data => data.ValidationItems)
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
                .StartWith<CollectImage>()
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
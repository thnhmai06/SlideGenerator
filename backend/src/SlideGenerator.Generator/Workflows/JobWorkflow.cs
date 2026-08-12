/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobWorkflow.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Steps;
using SlideGenerator.Generator.Models.Data;
using WorkflowCore.Interface;

namespace SlideGenerator.Generator.Workflows;

/// <summary>Identifies the execution phase of a job's generation workflow.</summary>
public enum Phase
{
    /// <summary>Output cleanup.</summary>
    Preparation,

    /// <summary>Slide generation (worksheet → output presentation).</summary>
    Generation
}

/// <summary>
///     Orchestrates the generation of a single job via WorkflowCore. One instance of this workflow is
///     spawned per job by <c>Service.StartAsync</c> (plain code, not itself a WorkflowCore workflow) —
///     this is the only workflow type registered with WorkflowCore, so
///     <c>WorkflowOptions.MaxConcurrentWorkflows</c> caps exactly the RAM-heavy per-job work
///     (Workbook/Presentation instances held in memory) without throttling the acceptance of new
///     generation requests.
///     <see cref="PreflightCleanup" /> → <see cref="InspectUrlsStep" /> → <see cref="GenerateJobStep" />
/// </summary>
public sealed class JobWorkflow : IWorkflow<JobContext>
{
    /// <inheritdoc />
    public string Id => nameof(JobWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public void Build(IWorkflowBuilder<JobContext> builder)
    {
        builder
            .StartWith<PreflightCleanup>()
            .Then<InspectUrlsStep>()
            .Then<GenerateJobStep>();
    }
}

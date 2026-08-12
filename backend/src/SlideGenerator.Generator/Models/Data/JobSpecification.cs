/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobSpecification.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Models.Data;

/// <summary>Source workbook and worksheet node ids.</summary>
public sealed record WorkbookRef(string WorkbookNodeId, string WorksheetNodeId);

/// <summary>Template presentation and slide node ids.</summary>
public sealed record PresentationRef(string PresentationNodeId, string SlideNodeId);

/// <summary>
///     Represents a single slide generation unit: one worksheet mapped to one slide via one map node.
///     Persisted as <see cref="JobPersistContext.Specification" /> by WorkflowCore for the single
///     <see cref="Workflows.JobWorkflow" /> instance spawned for this job.
/// </summary>
public sealed class JobSpecification
{
    /// <summary>Source workbook and worksheet node ids.</summary>
    public WorkbookRef Source { get; init; } = null!;

    /// <summary>Template presentation and slide node ids.</summary>
    public PresentationRef Template { get; init; } = null!;

    /// <summary>Key of the <see cref="Recipe.Models.MapNode" /> in <c>Recipe.Nodes</c>.</summary>
    public string MapNodeId { get; init; } = null!;

    /// <summary>Absolute path to the output presentation file for this job.</summary>
    public string OutputPath { get; init; } = null!;
}

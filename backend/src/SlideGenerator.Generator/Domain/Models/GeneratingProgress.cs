/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingProgress.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Application.Workflows;

namespace SlideGenerator.Generator.Domain.Models;

/// <summary>
///     Represents the progress information of a running workflow.
/// </summary>
public sealed record GeneratingProgress
{
    /// <summary>Gets the workflow instance identifier that this event belongs to.</summary>
    public string WorkflowInstanceId { get; init; } = string.Empty;

    /// <summary>Gets the lifecycle event type.</summary>
    public GeneratingEvent Event { get; init; }

    /// <summary>
    ///     Gets the class name of the completed step (e.g., <c>"DownloadImage"</c>),
    ///     or <see langword="null" /> for workflow-level events.
    /// </summary>
    public string? StepName { get; init; }

    /// <summary>
    ///     Gets the workflow phase the step belongs to,
    ///     or <see langword="null" /> for workflow-level events.
    /// </summary>
    public GeneratingPhase? Phase { get; init; }

    /// <summary>Gets the current workflow execution status.</summary>
    public GeneratingStatus Status { get; init; }

    /// <summary>Gets the UTC timestamp when this event was emitted.</summary>
    public DateTimeOffset Timestamp { get; init; }
}
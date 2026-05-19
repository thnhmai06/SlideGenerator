/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingProgress.cs
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
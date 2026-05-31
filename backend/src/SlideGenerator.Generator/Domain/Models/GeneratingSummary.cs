/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: GeneratingSummary.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Domain.Models;

/// <summary>
///     Lightweight snapshot of a workflow instance returned by <c>generating.active.list</c>,
///     <c>generating.active.query</c>, <c>generating.completed.list</c>, and
///     <c>generating.completed.query</c> IPC methods.
/// </summary>
public sealed record GeneratingSummary
{
    /// <summary>Gets the unique workflow instance identifier.</summary>
    public required string InstanceId { get; init; }

    /// <summary>Gets the display name of the generation job, if available.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the recipe id used by this workflow instance.</summary>
    public int RecipeId { get; init; }

    /// <summary>Gets the current execution status.</summary>
    public required GeneratingStatus Status { get; init; }

    /// <summary>Gets the UTC timestamp when the workflow was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the UTC timestamp when the workflow finished or was terminated, if applicable.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
}
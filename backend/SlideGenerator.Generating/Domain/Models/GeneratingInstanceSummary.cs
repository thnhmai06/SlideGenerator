/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: GeneratingInstanceSummary.cs
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

namespace SlideGenerator.Generating.Domain.Models;

/// <summary>
///     Lightweight snapshot of a workflow instance returned by <c>generating.active.list</c>,
///     <c>generating.active.query</c>, <c>generating.completed.list</c>, and
///     <c>generating.completed.query</c> IPC methods.
/// </summary>
public sealed record GeneratingInstanceSummary
{
    /// <summary>Gets the unique workflow instance identifier.</summary>
    public required string InstanceId { get; init; }

    /// <summary>Gets the display name of the generation job, if available.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the current execution status.</summary>
    public required GeneratingStatus Status { get; init; }

    /// <summary>Gets the UTC timestamp when the workflow was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets the UTC timestamp when the workflow finished or was terminated, if applicable.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: GeneratingCompletedHandler.cs
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
using SlideGenerator.Generating.Application.Abstractions;
using SlideGenerator.Generating.Domain.Models;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>generating.completed.*</c> JSON-RPC methods: list and query.
///     Provides read-only access to workflow instances that have finished execution
///     (completed successfully, cancelled, or errored).
/// </summary>
public sealed class GeneratingCompletedHandler(IGeneratingService generatingService)
{
    /// <summary>
    ///     Returns summaries of all completed, cancelled, or errored workflow instances.
    /// </summary>
    public Task<IReadOnlyList<GeneratingInstanceSummary>> ListAsync(CancellationToken ct)
        => generatingService.ListCompletedAsync(ct);

    /// <summary>
    ///     Returns the summary of a specific completed workflow instance,
    ///     or <see langword="null" /> if not found.
    /// </summary>
    public Task<GeneratingInstanceSummary?> QueryAsync(string workflowInstanceId, CancellationToken ct)
        => generatingService.QueryAsync(workflowInstanceId, ct);

    /// <summary>
    ///     Permanently deletes a single completed or cancelled workflow instance and all its associated data.
    /// </summary>
    /// <returns><see langword="true" /> if deleted; <see langword="false" /> if not found or still active.</returns>
    public Task<bool> DeleteAsync(string workflowInstanceId, CancellationToken ct)
        => generatingService.DeleteAsync(workflowInstanceId, ct);

    /// <summary>
    ///     Permanently deletes all completed and cancelled workflow instances.
    /// </summary>
    /// <returns>The number of instances deleted.</returns>
    public Task<int> DeleteAllAsync(CancellationToken ct)
        => generatingService.DeleteAllCompletedAsync(ct);
}

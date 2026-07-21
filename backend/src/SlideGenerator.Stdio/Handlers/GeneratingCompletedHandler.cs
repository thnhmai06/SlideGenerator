/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: GeneratingCompletedHandler.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Data;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>generating.completed.*</c> JSON-RPC methods: list, delete, and deleteAll.
///     Provides read-only access to workflow instances that have finished execution
///     (completed successfully, cancelled, or errored).
/// </summary>
public sealed class GeneratingCompletedHandler(IService generatingService)
{
    /// <summary>
    ///     Returns summaries of all completed, cancelled, or errored workflow instances, keyed by request id.
    /// </summary>
    public Task<IReadOnlyDictionary<string, Summary>> ListAsync(CancellationToken ct)
    {
        return generatingService.ListCompletedAsync(ct);
    }

    /// <summary>
    ///     Permanently deletes a request and all its associated data. If the request is still active
    ///     (running or paused), it is stopped first.
    /// </summary>
    /// <returns><see langword="true" /> if deleted; <see langword="false" /> if not found.</returns>
    public Task<bool> DeleteAsync(string workflowInstanceId, CancellationToken ct)
    {
        return generatingService.DeleteAsync(workflowInstanceId, ct);
    }

    /// <summary>
    ///     Permanently deletes all completed and cancelled workflow instances.
    /// </summary>
    /// <returns>The number of instances deleted.</returns>
    public Task<int> DeleteAllAsync(CancellationToken ct)
    {
        return generatingService.DeleteAllCompletedAsync(ct);
    }
}
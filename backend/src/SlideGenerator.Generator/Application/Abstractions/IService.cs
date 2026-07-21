/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Domain.Models.Data;

namespace SlideGenerator.Generator.Application.Abstractions;

/// <summary>
///     Defines the facade for workflow lifecycle operations exposed to the IPC layer.
///     Consumers (e.g. <c>GeneratingActiveHandler</c>) depend only on this interface and are
///     completely decoupled from WorkflowCore internals.
/// </summary>
public interface IService
{
    /// <summary>
    ///     Registers the workflow definition and starts the underlying workflow host.
    ///     Must be called once during application startup before any other method.
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    ///     Stops the underlying workflow host gracefully.
    ///     Must be called during application shutdown.
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);

    /// <summary>
    ///     Starts a new slide-generation request for the given <paramref name="request" /> — spawns one
    ///     job workflow instance per computed job. Multiple active requests for the same recipe are
    ///     allowed; throws only if a computed job's output path collides with one already claimed by
    ///     another active (running or paused) request's job.
    /// </summary>
    /// <returns>The generated request id grouping every job workflow instance spawned for it.</returns>
    Task<string> CreateAsync(Request request, CancellationToken ct = default);

    /// <summary>
    ///     Terminates every job workflow instance belonging to <paramref name="requestId" /> that is still
    ///     runnable or suspended. Best-effort — jobs already complete/terminated are counted as skipped.
    /// </summary>
    Task<PartialResult> StopAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Suspends every job workflow instance belonging to <paramref name="requestId" /> that is still
    ///     runnable. Best-effort — jobs not currently runnable are counted as skipped.
    /// </summary>
    Task<PartialResult> PauseAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Resumes every job workflow instance belonging to <paramref name="requestId" /> that is currently
    ///     suspended. Best-effort — jobs not currently suspended are counted as skipped.
    /// </summary>
    Task<PartialResult> ResumeAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Stops every currently active (running or paused) request. Best-effort.
    /// </summary>
    /// <returns>The number of requests successfully stopped.</returns>
    Task<int> StopAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Pauses every currently running (non-paused) request. Best-effort.
    /// </summary>
    /// <returns>The number of requests successfully paused.</returns>
    Task<int> PauseAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Returns summaries of all currently active (running or paused) requests, keyed by request id.
    /// </summary>
    Task<IReadOnlyDictionary<string, Summary>> ListActiveAsync(CancellationToken ct = default);

    /// <summary>
    ///     Returns summaries of all completed, canceled, or errored requests, keyed by request id.
    /// </summary>
    Task<IReadOnlyDictionary<string, Summary>> ListCompletedAsync(CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes every job workflow instance belonging to a request. If the request is still
    ///     active (running or paused), it is stopped first.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the request was found and deleted;
    ///     <see langword="false" /> if not found.
    /// </returns>
    Task<bool> DeleteAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes all completed and canceled requests and their associated data.
    /// </summary>
    /// <returns>The number of requests deleted.</returns>
    Task<int> DeleteAllCompletedAsync(CancellationToken ct = default);
}
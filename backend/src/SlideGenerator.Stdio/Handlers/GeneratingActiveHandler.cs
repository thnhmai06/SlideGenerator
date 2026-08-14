/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: GeneratingActiveHandler.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Stdio.Handlers;

/// <summary>
///     Handles all <c>generator.active.*</c> JSON-RPC methods: create, stop, pause, resume, and list.
///     Delegates execution to <see cref="IService" />, which publishes lifecycle/progress events
///     to <see cref="IEventBus" /> for forwarding to the client as notifications.
/// </summary>
/// <remarks>
///     This handler is intentionally thin: it performs no business logic — only parameter mapping.
/// </remarks>
public sealed class GeneratingActiveHandler(IService generatingService)
{
    /// <summary>
    ///     Creates a new slide generation request from the provided request and returns its request ID.
    ///     The <c>JobStarted</c> event is published by <see cref="IService" /> via the lifecycle handler.
    /// </summary>
    public Task<string> CreateAsync(Request request, CancellationToken ct)
    {
        return generatingService.CreateAsync(request, ct);
    }

    /// <summary>
    ///     Stops every job of the request. Progress notifications per job are published by
    ///     <see cref="IService.StopAsync" /> itself.
    /// </summary>
    public async Task<bool> StopAsync(string requestId, CancellationToken ct)
    {
        var result = await generatingService.StopAsync(requestId, ct).ConfigureAwait(false);
        return result.Succeeded > 0;
    }

    /// <summary>
    ///     Suspends every runnable job of the request.
    /// </summary>
    public async Task<bool> PauseAsync(string requestId, CancellationToken ct)
    {
        var result = await generatingService.PauseAsync(requestId, ct).ConfigureAwait(false);
        return result.Succeeded > 0;
    }

    /// <summary>
    ///     Resumes every suspended job of the request.
    /// </summary>
    public async Task<bool> ResumeAsync(string requestId, CancellationToken ct)
    {
        var result = await generatingService.ResumeAsync(requestId, ct).ConfigureAwait(false);
        return result.Succeeded > 0;
    }

    /// <summary>
    ///     Stops all currently active (running and paused) requests.
    /// </summary>
    /// <returns>The number of requests successfully stopped.</returns>
    public Task<int> StopAllAsync(CancellationToken ct)
    {
        return generatingService.StopAllAsync(ct);
    }

    /// <summary>
    ///     Pauses all currently running (non-paused) requests.
    /// </summary>
    /// <returns>The number of requests successfully paused.</returns>
    public Task<int> PauseAllAsync(CancellationToken ct)
    {
        return generatingService.PauseAllAsync(ct);
    }

    /// <summary>
    ///     Returns summaries of all currently active (running or paused) requests, keyed by request id.
    /// </summary>
    public Task<IReadOnlyDictionary<string, Summary>> ListAsync(CancellationToken ct)
    {
        return generatingService.ListActiveAsync(ct);
    }
}
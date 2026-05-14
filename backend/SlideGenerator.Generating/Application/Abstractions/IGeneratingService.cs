/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: IGeneratingService.cs
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
using SlideGenerator.Generating.Domain.Models;

namespace SlideGenerator.Generating.Application.Abstractions;

/// <summary>
///     Defines the facade for workflow lifecycle operations exposed to the IPC layer.
///     Consumers (e.g. <c>WorkflowHandler</c>) depend only on this interface and are
///     completely decoupled from WorkflowCore internals.
/// </summary>
public interface IGeneratingService
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
    ///     Starts a new slide-generation workflow for the given <paramref name="request" />.
    /// </summary>
    /// <returns>The unique workflow instance identifier.</returns>
    Task<string> StartAsync(GeneratingRequest request, CancellationToken ct = default);

    /// <summary>
    ///     Permanently terminates a running workflow instance.
    /// </summary>
    /// <returns><see langword="true" /> if the termination was accepted.</returns>
    Task<bool> CancelAsync(string instanceId, CancellationToken ct = default);

    /// <summary>
    ///     Suspends a running workflow instance. It can be resumed later via <see cref="ResumeAsync" />.
    /// </summary>
    /// <returns><see langword="true" /> if the suspension was accepted.</returns>
    Task<bool> PauseAsync(string instanceId, CancellationToken ct = default);

    /// <summary>
    ///     Resumes a previously suspended workflow instance.
    /// </summary>
    /// <returns><see langword="true" /> if the resumption was accepted.</returns>
    Task<bool> ResumeAsync(string instanceId, CancellationToken ct = default);
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: WorkflowProgressObserver.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using SlideGenerator.Generator.Domain.Models;
using StreamJsonRpc;

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     Subscribes to <see cref="GeneratingEventBus.OnProgress" /> and forwards each event
///     as a <c>workflow/progress</c> JSON-RPC notification via the active <see cref="JsonRpc" /> connection.
///     The connection is bound at runtime via <see cref="Attach" /> rather than at construction time
///     because <see cref="JsonRpc" /> is created after the DI container is built.
/// </summary>
internal sealed class WorkflowProgressObserver(ILogger<WorkflowProgressObserver> logger)
{
    private JsonRpc? _jsonRpc;

    /// <summary>
    ///     Attaches this observer to the supplied <see cref="GeneratingEventBus" /> and binds it
    ///     to the active JSON-RPC connection. Must be called before the first workflow is started
    ///     so no events are missed.
    /// </summary>
    /// <param name="bus">The event bus to subscribe to.</param>
    /// <param name="jsonRpc">The active JSON-RPC connection used to send notifications to the client.</param>
    public void Attach(GeneratingEventBus bus, JsonRpc jsonRpc)
    {
        _jsonRpc = jsonRpc;
        bus.OnProgress += Handle;
    }

    /// <summary>
    ///     Detaches this observer from the supplied <see cref="GeneratingEventBus" />.
    ///     Call during graceful shutdown before the host is stopped.
    /// </summary>
    /// <param name="bus">The event bus to unsubscribe from.</param>
    public void Detach(GeneratingEventBus bus)
    {
        bus.OnProgress -= Handle;
    }

    private void Handle(GeneratingProgress progress)
    {
        if (_jsonRpc is null)
            return;

        _ = _jsonRpc.NotifyWithParameterObjectAsync("workflow/progress", progress)
            .ContinueWith(
                t => logger.LogWarning(t.Exception!, "Failed to send workflow/progress notification."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }
}
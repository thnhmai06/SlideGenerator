/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: WorkflowProgressObserver.cs
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
using SlideGenerator.Logging.Domain.Abstractions;
using SlideGenerator.Generating.Domain.Models;
using StreamJsonRpc;

namespace SlideGenerator.Ipc.Infrastructure;

/// <summary>
///     Subscribes to <see cref="GeneratingEventBus.OnProgress" /> and forwards each event
///     as a <c>workflow/progress</c> JSON-RPC notification via the active <see cref="JsonRpc" /> connection.
///     The connection is bound at runtime via <see cref="Attach" /> rather than at construction time
///     because <see cref="JsonRpc" /> is created after the DI container is built.
/// </summary>
public sealed class WorkflowProgressObserver(ISystemLogger logger)
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
                t => logger.Warning(t.Exception!, "Failed to send workflow/progress notification."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }
}






/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: GeneratingEventBus.cs
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

namespace SlideGenerator.Ipc.Infrastructure;

/// <summary>
///     A lightweight, in-process event bus for workflow progress notifications.
///     Decouples the <see cref="Handlers.GeneratingActiveHandler" /> (publisher) from the
///     <see cref="WorkflowProgressObserver" /> (subscriber) without depending on
///     WorkflowCore-internal lifecycle hooks.
/// </summary>
internal sealed class GeneratingEventBus : IGeneratingEventBus
{
    /// <summary>
    ///     Publishes a <see cref="GeneratingProgress" /> to all current subscribers.
    ///     Safe to call from any thread.
    /// </summary>
    /// <param name="progress">The progress notification payload to deliver.</param>
    public void Publish(GeneratingProgress progress)
    {
        OnProgress?.Invoke(progress);
    }

    /// <summary>
    ///     Raised whenever a workflow lifecycle event occurs (start, complete, suspend, resume,
    ///     error). Subscribers must be registered before the first workflow is started.
    /// </summary>
    public event Action<GeneratingProgress>? OnProgress;
}
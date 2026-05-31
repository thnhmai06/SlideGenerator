/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: GeneratingEventBus.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models;

namespace SlideGenerator.Stdio.Implementations;

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
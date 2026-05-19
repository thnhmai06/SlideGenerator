/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IGeneratingEventBus.cs
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

using SlideGenerator.Generator.Domain.Models;

namespace SlideGenerator.Generator.Application.Abstractions;

/// <summary>Identifies lifecycle events published by the generating workflow.</summary>
public enum GeneratingEvent
{
    WorkflowStarted,
    WorkflowCompleted,
    WorkflowSuspended,
    WorkflowResumed,
    WorkflowCancelled,
    WorkflowError,
    StepCompleted
}

/// <summary>
///     Defines a publish-only event bus for broadcasting workflow lifecycle events
///     (started, suspended, resumed, completed, crashed) to registered observers.
/// </summary>
/// <remarks>
///     <para>
///         This interface is owned by the <c>Pipeline</c> module (the publisher).
///         The concrete implementation — <c>WorkflowEventBus</c> — lives in <c>SlideGenerator.Ipc</c>
///         and forwards events as <c>workflow/progress</c> JSON-RPC notifications.
///     </para>
///     <para>
///         This decoupling satisfies <c>dep-interface-ownership</c>: the interface belongs to
///         the module that uses it, not the module that implements it.
///         <c>SlideGenerator.Ipc</c> depends inward on <c>SlideGenerator.Pipeline</c>;
///         <c>SlideGenerator.Pipeline</c> never references <c>SlideGenerator.Ipc</c>.
///     </para>
/// </remarks>
public interface IGeneratingEventBus
{
    /// <summary>
    ///     Publishes a <see cref="GeneratingProgress" /> event to all registered observers.
    ///     Safe to call from any thread.
    /// </summary>
    void Publish(GeneratingProgress progress);
}
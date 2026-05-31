/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IGeneratingEventBus.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
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
///         The concrete implementation — <c>WorkflowEventBus</c> — lives in <c>SlideGenerator.Stdio</c>
///         and forwards events as <c>workflow/progress</c> JSON-RPC notifications.
///     </para>
///     <para>
///         This decoupling satisfies <c>dep-interface-ownership</c>: the interface belongs to
///         the module that uses it, not the module that implements it.
///         <c>SlideGenerator.Stdio</c> depends inward on <c>SlideGenerator.Pipeline</c>;
///         <c>SlideGenerator.Pipeline</c> never references <c>SlideGenerator.Stdio</c>.
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
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: IEventBus.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Workflows.Models;

namespace SlideGenerator.Generator.Progress;

/// <summary>
///     Defines a publish-only event bus for broadcasting Workflow/Job/Row progress notifications to registered
///     observers.
/// </summary>
/// <remarks>
///     <para>
///         This interface is owned by the <c>Generator</c> module (the publisher). The concrete implementation
///         lives in <c>SlideGenerator.Desktop</c> and raises the events the UI's progress hub subscribes to.
///     </para>
///     <para>
///         This decoupling satisfies <c>dep-interface-ownership</c>: the interface belongs to the module that
///         uses it, not the module that implements it. <c>SlideGenerator.Desktop</c> depends inward on
///         <c>SlideGenerator.Generator</c>; <c>SlideGenerator.Generator</c> never references it.
///     </para>
/// </remarks>
public interface IEventBus
{
    /// <summary>Publishes a <see cref="WorkflowSnapshot" /> event to all registered observers.</summary>
    void Publish(WorkflowSnapshot workflow);

    /// <summary>Publishes a <see cref="JobSnapshot" /> event to all registered observers.</summary>
    void Publish(JobSnapshot job);

    /// <summary>Publishes a <see cref="RowProgress" /> event to all registered observers.</summary>
    void Publish(RowProgress progress);
}

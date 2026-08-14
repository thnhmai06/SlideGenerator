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

namespace SlideGenerator.Generator.Progress;

/// <summary>
///     Defines a publish-only event bus for broadcasting Request/Job/Row progress notifications
///     to registered observers.
/// </summary>
/// <remarks>
///     <para>
///         This interface is owned by the <c>Generator</c> module (the publisher).
///         The concrete implementation lives in <c>SlideGenerator.Stdio</c> and forwards events as
///         <c>progress/request</c>/<c>progress/jobs</c>/<c>progress/rows</c> JSON-RPC notifications.
///     </para>
///     <para>
///         This decoupling satisfies <c>dep-interface-ownership</c>: the interface belongs to
///         the module that uses it, not the module that implements it.
///         <c>SlideGenerator.Stdio</c> depends inward on <c>SlideGenerator.Generator</c>;
///         <c>SlideGenerator.Generator</c> never references <c>SlideGenerator.Stdio</c>.
///     </para>
/// </remarks>
public interface IEventBus
{
    /// <summary>Publishes a <see cref="RequestProgress" /> event to all registered observers.</summary>
    void Publish(RequestProgress progress);

    /// <summary>Publishes a <see cref="JobSnapshot" /> event to all registered observers.</summary>
    void Publish(JobSnapshot job);

    /// <summary>Publishes a <see cref="RowProgress" /> event to all registered observers.</summary>
    void Publish(RowProgress progress);

    /// <summary>
    ///     Announces the total number of jobs about to be spawned for <paramref name="requestId" />,
    ///     called once before the spawn loop begins. Not itself a progress event (not persisted, not
    ///     forwarded as a notification) — purely a bookkeeping signal so a subscriber computing
    ///     <see cref="RequestPhase.ProcessingStarted" /> knows the true expected job count up front,
    ///     instead of inferring it from however many <see cref="JobSnapshot" /> events have arrived so
    ///     far (which races against the still-running spawn loop for multi-job requests).
    /// </summary>
    void AnnounceExpectedJobCount(string requestId, int count);
}
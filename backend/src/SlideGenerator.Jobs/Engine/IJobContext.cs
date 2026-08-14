/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: IJobContext.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>Engine-provided handle a workload uses to checkpoint and report progress.</summary>
public interface IJobContext<in TState>
{
    /// <summary>
    ///     <see langword="true" /> if this run is resuming a job found non-terminal at startup (crash
    ///     recovery), rather than a fresh start. Workloads that run one-time setup (e.g., clearing a
    ///     partially written output file) must skip it when this is <see langword="true" />.
    /// </summary>
    bool IsResume { get; }

    /// <summary>
    ///     Publishes an intermediate state. When <paramref name="durable" /> is <see langword="true" />,
    ///     the returned task completes only once the consumer's persistence has durably applied the state
    ///     (not merely buffered) — use this at points where a crash between the writer and a later buffered
    ///     flush would lose meaningful progress (e.g., a phase transition). Buffered
    ///     (<paramref name="durable" /> = <see langword="false" />) reports may be coalesced by the consumer.
    /// </summary>
    Task ReportAsync(TState state, bool durable = false, CancellationToken ct = default);

    /// <summary>
    ///     Blocks while the job is paused; returns immediately otherwise. Cooperative — never interrupts an
    ///     in-flight step, only a workload's own call to this method can observe a pause.
    /// </summary>
    Task CheckpointAsync(CancellationToken ct);
}

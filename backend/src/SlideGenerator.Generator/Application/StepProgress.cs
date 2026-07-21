/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: StepProgress.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Data;
using SlideGenerator.Generator.Domain.Models.Enum;

namespace SlideGenerator.Generator.Application;

/// <summary>
///     Binds a step's identity (request id, job id) once at the top of a step's <c>Run</c>/<c>RunAsync</c>
///     entry point — alongside where <c>_logger</c> is normally captured — then exposes logger-like
///     <see cref="ReportRow" />/<see cref="SeedRows" /> methods for publishing row-scoped progress inline
///     anywhere in that step's code, including partial-file helper methods, via a captured instance field.
///     Construct one instance per step execution via <see cref="From" />; this is not a DI-registered service.
/// </summary>
internal sealed class StepProgress
{
    private readonly IEventBus _eventBus;
    private readonly string _requestId;
    private readonly string _jobId;

    private StepProgress(IEventBus eventBus, string requestId, string jobId)
    {
        _eventBus = eventBus;
        _requestId = requestId;
        _jobId = jobId;
    }

    /// <summary>
    ///     Creates a <see cref="StepProgress" /> bound to one-step execution. Call once at the top of
    ///     <c>Run</c>/<c>RunAsync</c>, next to where <c>_logger</c> is captured.
    /// </summary>
    internal static StepProgress From(IEventBus eventBus, string requestId, string jobId) =>
        new(eventBus, requestId, jobId);

    /// <summary>
    ///     Publishes a <see cref="RowProgress" /> for <paramref name="rowIndex" />. Fire-and-forget and
    ///     non-blocking on the calling thread (see <see cref="IEventBus.Publish(RowProgress)" />) — safe to
    ///     call as often as once per row/image, like a logger call.
    /// </summary>
    internal void ReportRow(int rowIndex, RowStatus status, RowStage stage = RowStage.None, string? note = null) =>
        _eventBus.Publish(new RowProgress
        {
            RequestId = _requestId,
            JobId = _jobId,
            RowIndex = rowIndex,
            Status = status,
            Stage = stage,
            Note = note,
            Timestamp = DateTimeOffset.UtcNow
        });

    /// <summary>
    ///     Pre-seeds every row in <paramref name="rowIndices" /> as <see cref="RowStatus.Waiting" /> before
    ///     the per-row loop begins, so the frontend sees the full row set immediately instead of it growing
    ///     one row at a time.
    /// </summary>
    internal void SeedRows(IEnumerable<int> rowIndices)
    {
        foreach (var rowIndex in rowIndices)
            ReportRow(rowIndex, RowStatus.Waiting);
    }
}

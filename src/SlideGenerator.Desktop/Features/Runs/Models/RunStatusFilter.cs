/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RunStatusFilter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Desktop.Features.Runs.Models;

/// <summary>
///     Chip filter over the one Runs list — <c>ListActiveAsync</c>/<c>ListCompletedAsync</c> already return
///     the identical <c>Summary</c> shape, differing only by aggregate <c>JobStatus</c>, so "active" and
///     "completed" are filters on one list, not separate pages.
/// </summary>
public enum RunStatusFilter
{
    /// <summary>Every request, regardless of status.</summary>
    All,

    /// <summary>Requests whose aggregate status is Running.</summary>
    Running,

    /// <summary>Requests whose aggregate status is Paused.</summary>
    Paused,

    /// <summary>Requests whose aggregate status is Complete.</summary>
    Done,

    /// <summary>Requests whose aggregate status is Cancelled.</summary>
    Cancelled
}

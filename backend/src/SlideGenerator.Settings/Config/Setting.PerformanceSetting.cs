/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.PerformanceSetting.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Settings.Config;

public sealed partial record Setting
{
    /// <summary>
    ///     Setting related to the execution and orchestration of generation jobs.
    /// </summary>
    public sealed record PerformanceSetting
    {
        /// <summary>
        ///     Gets the maximum number of concurrently running per-job generation workflows across the app —
        ///     caps RAM usage from Workbook/Presentation instances held in memory during generation.
        /// </summary>
        public uint MaxConcurrentJobs { get; init; } = 5;
    }
}
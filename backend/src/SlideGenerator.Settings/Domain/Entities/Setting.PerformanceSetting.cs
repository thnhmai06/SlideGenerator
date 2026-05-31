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

namespace SlideGenerator.Settings.Domain.Entities;

public sealed partial record Setting
{
    /// <summary>
    ///     Setting related to the execution and orchestration of generation jobs.
    ///     Controls the degree of parallelism for different stages of the pipeline.
    /// </summary>
    public sealed record PerformanceSetting
    {
        /// <summary>Gets the maximum number of concurrent image downloads.</summary>
        public uint MaxParallelDownloadImage { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent image editing operations.</summary>
        public uint MaxParallelEditImage { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent presentation editing operations (slides filling).</summary>
        public uint MaxParallelEditPresentation { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent workbook reading operations.</summary>
        public uint MaxParallelReadWorkbook { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent presentation reading operations.</summary>
        public uint MaxParallelReadPresentation { get; init; } = 5;
    }
}
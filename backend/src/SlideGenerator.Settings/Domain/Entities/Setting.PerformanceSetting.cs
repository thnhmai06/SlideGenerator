/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.PerformanceSetting.cs
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
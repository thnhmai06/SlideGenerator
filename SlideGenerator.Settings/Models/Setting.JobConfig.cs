/*
 * Copyright (C) 2026 Thành Mai
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.JobConfig.cs
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

namespace SlideGenerator.Settings.Models;

public sealed partial record Setting
{
    /// <summary>
    ///     Settings related to the execution and orchestration of generation jobs.
    ///     Controls the degree of parallelism for different stages of the pipeline.
    /// </summary>
    public sealed record JobSetting
    {
        /// <summary>Gets the maximum number of concurrent image downloads.</summary>
        public int MaxParallelDownloadImage { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent image editing operations.</summary>
        public int MaxParallelEditImage { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent presentation editing operations (slides filling).</summary>
        public int MaxParallelEditPresentation { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent workbook reading operations.</summary>
        public int MaxParallelReadWorkbook { get; init; } = 5;

        /// <summary>Gets the maximum number of concurrent presentation reading operations.</summary>
        public int MaxParallelReadPresentation { get; init; } = 5;
    }
}
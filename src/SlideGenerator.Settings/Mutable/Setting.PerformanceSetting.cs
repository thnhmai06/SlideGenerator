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

using System.Text.Json.Serialization;

namespace SlideGenerator.Settings.Mutable;

public sealed partial record Setting
{
    /// <summary>
    ///     Setting related to the execution and orchestration of generation workflows.
    /// </summary>
    public sealed record PerformanceSetting
    {
        /// <summary>
        ///     Gets the maximum number of concurrently running generation workflows across the app —
        ///     caps RAM usage from Workbook/Presentation instances held in memory during generation.
        /// </summary>
        public uint MaxConcurrentWorkflows { get; init; } = 5;

        /// <summary>
        ///     Reads <see cref="MaxConcurrentWorkflows" /> from the pre-rename settings key
        ///     (<c>MaxConcurrentJobs</c>) so an existing <c>UserSettings.json</c> keeps its value. Never written.
        /// </summary>
        [JsonPropertyName("MaxConcurrentJobs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? LegacyMaxConcurrentJobs
        {
            get => null;
            init
            {
                if (value is { } legacy) MaxConcurrentWorkflows = legacy;
            }
        }
    }
}
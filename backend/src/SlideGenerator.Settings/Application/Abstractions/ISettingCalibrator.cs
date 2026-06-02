/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: ISettingCalibrator.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Domain.Models;

namespace SlideGenerator.Settings.Application.Abstractions;

/// <summary>
///     Measures hardware capabilities and network conditions, then computes
///     optimal parallelism limits for the generation pipeline.
/// </summary>
public interface ISettingCalibrator
{
    /// <summary>
    ///     Probes the current machine's CPU, RAM, disk speed, network bandwidth, and latency,
    ///     then returns the raw measurements alongside the recommended
    ///     <see cref="Domain.Entities.Setting.PerformanceSetting" />.
    ///     Does not persist the result — callers use <see cref="ISettingManager.Update" /> to apply it.
    /// </summary>
    Task<PerformanceCalibration> CalibratePerformanceAsync(CancellationToken ct = default);
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: PerformanceCalibration.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Application.Services;
using SlideGenerator.Settings.Domain.Entities;

namespace SlideGenerator.Settings.Domain.Models;

/// <summary>
///     Raw hardware and network measurements collected before a calibration run.
/// </summary>
/// <param name="CpuCount">Logical CPU core count.</param>
/// <param name="RamGiB">Total available RAM in GiB.</param>
/// <param name="DiskMBps">
///     Sequential write speed in MB/s, or <see langword="null" /> if the disk probe failed.
///     When <see langword="null" />, <see cref="Application.Services.SettingTuner.TunePerformance" />
///     preserves the current <c>ReadWorkbook</c> and <c>ReadPresentation</c> gate values.
/// </param>
/// <param name="NetworkMbps">
///     Aggregate download bandwidth in Mbps, measured using
///     <see cref="Infrastructure.Services.SettingProbe.ParallelStreamCount" /> concurrent streams,
///     or <see langword="null" /> if the network probe failed.
///     When <see langword="null" />, <see cref="Application.Services.SettingTuner.TunePerformance" />
///     preserves the current <c>DownloadImage</c> gate value.
/// </param>
/// <param name="LatencyMs">
///     Median RTT to cloud storage endpoints in milliseconds, or <see langword="null" /> if
///     no latency samples could be collected.
///     When <see langword="null" />, <see cref="Application.Services.SettingTuner.TunePerformance" />
///     preserves the current <c>DownloadImage</c> gate value.
/// </param>
/// <param name="SingleStreamMbps">
///     Per-connection download throughput in Mbps, measured with a single stream,
///     or <see langword="null" /> if the single-stream probe failed.
///     When <see langword="null" />, <see cref="Application.Services.SettingTuner" /> falls back to
///     <see cref="SettingTuner.RAssumedMbps" /> (assumed Drive throttle) rather than the current setting.
/// </param>
public sealed record ProbeResult(
    int CpuCount,
    double RamGiB,
    double? DiskMBps,
    double? NetworkMbps,
    double? LatencyMs,
    double? SingleStreamMbps = null);

/// <summary>
///     Raw measurements and the derived performance configuration from a calibration run.
/// </summary>
public sealed record PerformanceCalibration(ProbeResult Result, Setting.PerformanceSetting RecommendSetting);
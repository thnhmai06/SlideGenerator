/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: SettingCalibrator.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Application.Services;
using SlideGenerator.Settings.Domain.Models;

namespace SlideGenerator.Settings.Infrastructure.Services;

/// <summary>
///     Orchestrates hardware and network probing to produce a recommended
///     <see cref="Domain.Entities.Setting.PerformanceSetting" />.
/// </summary>
public sealed class SettingCalibrator(ISettingProvider settingProvider) : ISettingCalibrator
{
    /// <inheritdoc />
    public async Task<PerformanceCalibration> CalibratePerformanceAsync(CancellationToken ct = default)
    {
        var probe = await SettingProbe
            .ProbePerformanceAsync(settingProvider.Current.Network, ct)
            .ConfigureAwait(false);

        var performance = SettingTuner.TunePerformance(probe, settingProvider.Current.Performance);
        return new PerformanceCalibration(probe, performance);
    }
}
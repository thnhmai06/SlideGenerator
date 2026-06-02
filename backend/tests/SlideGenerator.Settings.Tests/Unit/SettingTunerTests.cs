/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings.Tests
 * File: SettingTunerTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Settings.Application.Services;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Models;
using Xunit;

namespace SlideGenerator.Settings.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SettingTuner" />, verifying that the calibration
///     formula maps hardware and network probe measurements to valid, bounded parallelism limits,
///     and that null probe inputs fall back to the provided current setting or assumed constants.
/// </summary>
public sealed class SettingTunerTests
{
    private static readonly Setting.PerformanceSetting DefaultCurrent = new();

    /// <summary>
    ///     All gate values must be at least 1 regardless of input.
    /// </summary>
    [Fact]
    public void TunePerformance_AlwaysReturnsAtLeastOne()
    {
        var probe = new ProbeResult(1, 1, 10, 0, 5000);

        var result = SettingTuner.TunePerformance(probe, DefaultCurrent);

        result.MaxParallelDownloadImage.Should().BeGreaterThanOrEqualTo(1);
        result.MaxParallelEditImage.Should().BeGreaterThanOrEqualTo(1);
        result.MaxParallelEditPresentation.Should().BeGreaterThanOrEqualTo(1);
        result.MaxParallelReadWorkbook.Should().BeGreaterThanOrEqualTo(1);
        result.MaxParallelReadPresentation.Should().BeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    ///     All gate values must not exceed their configured caps.
    /// </summary>
    [Fact]
    public void TunePerformance_NeverExceedsCaps()
    {
        var probe = new ProbeResult(64, 256, 10_000, 10_000, 1000);

        var result = SettingTuner.TunePerformance(probe, DefaultCurrent);

        result.MaxParallelDownloadImage.Should().BeLessThanOrEqualTo(32);
        result.MaxParallelEditImage.Should().BeLessThanOrEqualTo(12);
        result.MaxParallelEditPresentation.Should().BeLessThanOrEqualTo(4);
        result.MaxParallelReadWorkbook.Should().BeLessThanOrEqualTo(6);
        result.MaxParallelReadPresentation.Should().BeLessThanOrEqualTo(5);
    }

    /// <summary>
    ///     Higher latency should produce more <c>MaxParallelDownloadImage</c> slots
    ///     because latency-dominated I/O benefits from deeper request queues.
    /// </summary>
    [Fact]
    public void TunePerformance_HigherLatency_IncreasesDownloadParallelism()
    {
        var probeLow = new ProbeResult(8, 16, 500, 100, 20);
        var probeHigh = new ProbeResult(8, 16, 500, 100, 400);

        var resultLow = SettingTuner.TunePerformance(probeLow, DefaultCurrent);
        var resultHigh = SettingTuner.TunePerformance(probeHigh, DefaultCurrent);

        resultHigh.MaxParallelDownloadImage.Should().BeGreaterThan(resultLow.MaxParallelDownloadImage);
    }

    /// <summary>
    ///     Higher CPU count should produce more <c>MaxParallelEditImage</c> slots (up to the cap).
    /// </summary>
    [Fact]
    public void TunePerformance_MoreCpu_IncreasesEditImageParallelism()
    {
        var probe2 = new ProbeResult(2, 16, 500, 100, 80);
        var probe8 = new ProbeResult(8, 16, 500, 100, 80);

        var result2 = SettingTuner.TunePerformance(probe2, DefaultCurrent);
        var result8 = SettingTuner.TunePerformance(probe8, DefaultCurrent);

        result8.MaxParallelEditImage.Should().BeGreaterThan(result2.MaxParallelEditImage);
    }

    /// <summary>
    ///     Zero network speed (offline / probe returned 0) should still compute a value within bounds.
    ///     Zero is not <see langword="null" /> — the formula still runs and produces the baseline of 2.
    /// </summary>
    [Fact]
    public void TunePerformance_ZeroNetworkSpeed_ReturnsWithinBounds()
    {
        var probe = new ProbeResult(4, 8, 500, 0, 50);

        var result = SettingTuner.TunePerformance(probe, DefaultCurrent);

        result.MaxParallelDownloadImage.Should().BeGreaterThanOrEqualTo(2).And.BeLessThanOrEqualTo(32);
    }

    /// <summary>
    ///     When per-connection throughput (r) is low relative to pipe bandwidth (B) — as is typical
    ///     with Google Drive per-stream throttle — the formula should open enough parallel connections
    ///     to saturate the pipe (≈ B/r), regardless of latency.
    /// </summary>
    [Theory]
    [InlineData(20, 12, 200, 4)] // corporate 20 Mbps, Drive r=12 Mbps → B/r ≈ 1.7, low-latency term
    [InlineData(100, 12, 80, 10)] // home 100 Mbps, Drive r=12 Mbps → B/r ≈ 8.3
    [InlineData(1000, 12, 10, 32)] // gigabit, Drive r=12 Mbps → B/r ≈ 83, capped at 32
    public void TunePerformance_DriveThrottledConnections_ScalesWithBOverR(
        double networkMbps, double singleStreamMbps, double latencyMs, uint expectedMinSlots)
    {
        var probe = new ProbeResult(8, 16, 500, networkMbps, latencyMs, singleStreamMbps);

        var result = SettingTuner.TunePerformance(probe, DefaultCurrent);

        result.MaxParallelDownloadImage.Should().BeGreaterThanOrEqualTo(expectedMinSlots);
    }

    /// <summary>
    ///     When <see cref="ProbeResult.SingleStreamMbps" /> is <see langword="null" /> (probe failed),
    ///     the formula falls back to <see cref="SettingTuner.RAssumedMbps" /> — not to the current
    ///     setting — because the Drive throttle constant is a reliable substitute.
    /// </summary>
    [Fact]
    public void TunePerformance_SingleStreamMbpsNull_FallsBackToAssumedR()
    {
        var probeWithMeasured = new ProbeResult(4, 8, 300, 100, 50, SettingTuner.RAssumedMbps);
        var probeWithNull = new ProbeResult(4, 8, 300, 100, 50); // SingleStreamMbps defaults to null

        var withMeasured = SettingTuner.TunePerformance(probeWithMeasured, DefaultCurrent);
        var withNull = SettingTuner.TunePerformance(probeWithNull, DefaultCurrent);

        withNull.MaxParallelDownloadImage.Should().Be(withMeasured.MaxParallelDownloadImage);
    }

    /// <summary>
    ///     When <see cref="ProbeResult.NetworkMbps" /> is <see langword="null" /> (network probe failed),
    ///     the download gate must equal the value from the provided current setting.
    /// </summary>
    [Fact]
    public void TunePerformance_NullNetwork_PreservesCurrentDownloadGate()
    {
        var probe = new ProbeResult(4, 8, 300, null, null);
        var current = new Setting.PerformanceSetting { MaxParallelDownloadImage = 7u };

        var result = SettingTuner.TunePerformance(probe, current);

        result.MaxParallelDownloadImage.Should().Be(7u);
    }

    /// <summary>
    ///     When <see cref="ProbeResult.DiskMBps" /> is <see langword="null" /> (disk probe failed),
    ///     the read gates must equal the values from the provided current setting.
    /// </summary>
    [Fact]
    public void TunePerformance_NullDisk_PreservesCurrentReadGates()
    {
        var probe = new ProbeResult(4, 8, null, 100, 50);
        var current = new Setting.PerformanceSetting
        {
            MaxParallelReadWorkbook = 3u,
            MaxParallelReadPresentation = 2u
        };

        var result = SettingTuner.TunePerformance(probe, current);

        result.MaxParallelReadWorkbook.Should().Be(3u);
        result.MaxParallelReadPresentation.Should().Be(2u);
    }
}
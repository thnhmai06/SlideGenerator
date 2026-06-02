/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings.Tests
 * File: SettingProbeTests.cs
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
using SlideGenerator.Settings.Infrastructure.Services;
using Xunit;

namespace SlideGenerator.Settings.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="SettingProbe" />, verifying that hardware measurements
///     are always populated, disk/network measurements degrade gracefully to <see langword="null" />
///     on failure, and probe results flow into <see cref="SettingTuner" /> without errors.
///     Tests marked <c>SlowIntegration</c> require a live internet connection; exclude them
///     in offline CI via <c>--filter "Category!=SlowIntegration"</c>.
/// </summary>
public sealed class SettingProbeTests
{
    /// <summary>Network setting with a 1-second timeout; no proxy required.</summary>
    private static readonly Setting.NetworkSetting ShortTimeoutNetwork =
        new() { Retry = new Setting.RetrySetting { Timeout = 1 } };

    /// <summary>Default network setting (30-second timeout); no proxy required.</summary>
    private static readonly Setting.NetworkSetting DefaultNetwork = new();

    /// <summary>
    ///     CPU count and available RAM are read synchronously from the OS before any I/O,
    ///     so they must always be positive regardless of network or disk state.
    /// </summary>
    [Fact]
    public async Task ProbePerformance_HardwareFields_AlwaysPositive()
    {
        var result =
            await SettingProbe.ProbePerformanceAsync(ShortTimeoutNetwork, TestContext.Current.CancellationToken);

        result.CpuCount.Should().BeGreaterThanOrEqualTo(1);
        result.RamGiB.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     The disk probe writes a 10 MB temp file; on any machine with a writable temp directory,
    ///     it must return a positive MB/s value.
    /// </summary>
    [Fact]
    public async Task ProbePerformance_DiskProbe_ReturnsPositiveValue()
    {
        var result =
            await SettingProbe.ProbePerformanceAsync(ShortTimeoutNetwork, TestContext.Current.CancellationToken);

        result.DiskMBps.Should().NotBeNull().And.BeGreaterThan(0);
    }

    /// <summary>
    ///     When the cancellation token is already canceled before any I/O begins,
    ///     all I/O-dependent fields (<see cref="Domain.Models.ProbeResult.DiskMBps" />,
    ///     <see cref="Domain.Models.ProbeResult.NetworkMbps" />,
    ///     <see cref="Domain.Models.ProbeResult.SingleStreamMbps" />,
    ///     <see cref="Domain.Models.ProbeResult.LatencyMs" />) must be <see langword="null" />.
    ///     CPU and RAM are synchronous and remain populated.
    /// </summary>
    [Fact]
    public async Task ProbePerformance_PreCancelledToken_IoFieldsAreNull()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await SettingProbe.ProbePerformanceAsync(ShortTimeoutNetwork, cts.Token);

        result.CpuCount.Should().BeGreaterThanOrEqualTo(1);
        result.RamGiB.Should().BeGreaterThan(0);
        result.DiskMBps.Should().BeNull();
        result.NetworkMbps.Should().BeNull();
        result.SingleStreamMbps.Should().BeNull();
        result.LatencyMs.Should().BeNull();
    }

    /// <summary>
    ///     A <see cref="Domain.Models.ProbeResult" /> with all I/O fields <see langword="null" />
    ///     (as produced by cancellation) must flow into <see cref="SettingTuner.TunePerformance" />
    ///     without throwing, preserving current-setting fallback values for network- and disk-gated
    ///     parallelism limits.
    /// </summary>
    [Fact]
    public async Task ProbePerformance_NullIoFields_TunerPreservesCurrentGates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var current = new Setting.PerformanceSetting
        {
            MaxParallelDownloadImage = 7u,
            MaxParallelReadWorkbook = 3u,
            MaxParallelReadPresentation = 2u
        };

        var probe = await SettingProbe.ProbePerformanceAsync(ShortTimeoutNetwork, cts.Token);
        var act = () => SettingTuner.TunePerformance(probe, current);

        act.Should().NotThrow();
        var tuned = act();
        tuned.MaxParallelDownloadImage.Should().Be(current.MaxParallelDownloadImage);
        tuned.MaxParallelReadWorkbook.Should().Be(current.MaxParallelReadWorkbook);
        tuned.MaxParallelReadPresentation.Should().Be(current.MaxParallelReadPresentation);
        tuned.MaxParallelEditImage.Should().BeGreaterThanOrEqualTo(1);
        tuned.MaxParallelEditPresentation.Should().BeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    ///     Full probe against live cloud endpoints must return positive, bounded values for all
    ///     network measurements and produce a valid <see cref="Setting.PerformanceSetting" />
    ///     when fed into <see cref="SettingTuner" />.
    ///     Requires internet access — skip in offline CI environments.
    /// </summary>
    [Fact]
    [Trait("Category", "SlowIntegration")]
    public async Task ProbePerformance_ConnectedNetwork_ReturnsPositiveNetworkValues()
    {
        var result = await SettingProbe.ProbePerformanceAsync(DefaultNetwork, TestContext.Current.CancellationToken);

        result.NetworkMbps.Should().NotBeNull().And.BeGreaterThan(0);
        result.SingleStreamMbps.Should().NotBeNull().And.BeGreaterThan(0);
        result.LatencyMs.Should().NotBeNull().And.BeGreaterThan(0);

        var tuned = SettingTuner.TunePerformance(result, new Setting.PerformanceSetting());
        tuned.MaxParallelDownloadImage.Should().BeInRange(2, 32);
    }
}
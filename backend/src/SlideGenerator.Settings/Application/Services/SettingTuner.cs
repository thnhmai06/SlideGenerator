/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: SettingTuner.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Models;

namespace SlideGenerator.Settings.Application.Services;

/// <summary>
///     Pure formulas that map raw probe measurements to optimal configuration values.
///     Contains no I/O — all side effects are in <see cref="Infrastructure.Services.SettingProbe" />.
/// </summary>
internal static class SettingTuner
{
    /// <summary>
    ///     Assumed per-connection throughput cap in Mbps when <see cref="ProbeResult.SingleStreamMbps" />
    ///     is unavailable. Based on a typical Google Drive per-stream throttle (~1.5 MB/s = 12 Mbps).
    ///     Treat as a calibration constant — adjust if empirical Drive measurements differ.
    /// </summary>
    public const double RAssumedMbps = 12.0;

    /// <summary>
    ///     Assumed average image file size in Mbit (~1 MB) used in the latency-hiding term
    ///     <c>B × L / S</c> of the download parallelism formula.
    ///     Treat as a calibration constant.
    /// </summary>
    private const double SMbit = 8.0;

    /// <summary>
    ///     Computes the recommended <see cref="Setting.PerformanceSetting" /> from raw probe measurements.
    ///     Gates whose required probe inputs are <see langword="null" /> are left unchanged from
    ///     <paramref name="current" />.
    /// </summary>
    /// <param name="probe">Raw measurements from <see cref="Infrastructure.Services.SettingProbe.ProbePerformanceAsync" />.</param>
    /// <param name="current">
    ///     The active setting used as a fallback for any gate whose probe inputs are
    ///     <see langword="null" /> (e.g., when the network or disk probe failed).
    /// </param>
    /// <remarks>
    ///     <para>
    ///         Two shared factors are derived first.
    ///         The RAM headroom factor <c>rRam = clamp(RAM_GiB / 16, 0.6, 1.5)</c> scales with available
    ///         memory but is clipped to avoid extremes on very low or very high-memory machines.
    ///         The disk factor <c>d = clamp(log2(1 + D_MBps / 250), 0.5, 2.5)</c> is log-scaled so a fast
    ///         NVMe cannot dominate over other terms; when <see cref="ProbeResult.DiskMBps" /> is
    ///         <see langword="null" />, <c>d</c> is unavailable and the affected gates fall back to
    ///         <paramref name="current" />.
    ///     </para>
    ///     <para>
    ///         The download gate is network-bound and follows the bandwidth-delay model for fetching many
    ///         small files over an independently throttled cloud storage link (e.g., Google Drive caps each
    ///         stream at roughly 12 Mbps regardless of pipe speed):
    ///         <c>N_download = clamp(2 + B/r + B*L/S, 2, 32)</c>.
    ///         When <see cref="ProbeResult.NetworkMbps" /> or <see cref="ProbeResult.LatencyMs" /> is
    ///         <see langword="null" />, this gate preserves <paramref name="current" />.
    ///         <see cref="ProbeResult.SingleStreamMbps" /> being <see langword="null" /> only causes
    ///         fallback to <see cref="RAssumedMbps" /> (not to <paramref name="current" />), because the
    ///         Drive throttle constant is a reliable substitute regardless of the pipe quality.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Variable</term>
    ///             <description>Source and meaning</description>
    ///         </listheader>
    ///         <item>
    ///             <term>
    ///                 <c>B</c>
    ///             </term>
    ///             <description>
    ///                 <see cref="ProbeResult.NetworkMbps" /> — aggregate pipe bandwidth (Mbps),
    ///                 measured via <see cref="Infrastructure.Services.SettingProbe.ParallelStreamCount" /> parallel streams.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <c>r</c>
    ///             </term>
    ///             <description>
    ///                 <see cref="ProbeResult.SingleStreamMbps" />, or <see cref="RAssumedMbps" /> when
    ///                 <see langword="null" /> — per-connection throughput cap (Mbps).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <c>L</c>
    ///             </term>
    ///             <description>
    ///                 <see cref="ProbeResult.LatencyMs" /> / 1000 — round-trip latency in seconds.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <c>S</c>
    ///             </term>
    ///             <description>
    ///                 <see cref="SMbit" /> — assumed average image file size in Mbit (~1 MB).
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The remaining gates are CPU-bound. All scale with <c>sqrt(cpu)</c> to avoid
    ///         oversubscribing native thread pools (OpenCV, ImageMagick, Syncfusion):
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <c>N_editImage    = clamp(2 * sqrt(cpu) * rRam, 1, min(cpu, 12))</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>N_readWorkbook = clamp((1.2 + 0.45*d) * sqrt(cpu) * rRam, 1, 6)</c>
    ///                 — falls back to <paramref name="current" /> when <c>d</c> is unavailable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>N_readPresentation = clamp((1.0 + 0.35*d) * sqrt(cpu) * rRam, 1, 5)</c>
    ///                 — falls back to <paramref name="current" /> when <c>d</c> is unavailable.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <c>N_editPresentation = clamp(0.9 * sqrt(cpu) * rRam, 1, 4)</c> — low cap is
    ///                 intentional to avoid lock contention on the output file.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static Setting.PerformanceSetting TunePerformance(ProbeResult probe, Setting.PerformanceSetting current)
    {
        var cpu = Math.Max(1, probe.CpuCount);
        var ramFactor = Math.Clamp(probe.RamGiB / 16.0, 0.6, 1.5);

        var diskFactor = probe.DiskMBps is { } disk
            ? Math.Clamp(Math.Log2(1.0 + disk / 250.0), 0.5, 2.5)
            : (double?)null;

        #region Download (network-bound, bandwidth-delay model)

        // r: per-connection cap (measured or assumed Drive throttle — not current, Drive cap is reliable)
        var r = probe.SingleStreamMbps ?? RAssumedMbps;

        var downloadImage =
            // N = base + B/r + B×L/S
            probe is { NetworkMbps: { } b, LatencyMs: { } latencyMs } 
            ? Clamp(2.0 + b / r + b * (latencyMs / 1000.0) / SMbit, 2, 32) 
            : current.MaxParallelDownloadImage;

        #endregion

        #region CPU/native-memory-bound gates

        // sqrt(cpu) avoids oversubscribing OpenCV/ImageMagick native threads.
        var editImage = Clamp(
            Math.Sqrt(cpu) * 2.0 * ramFactor,
            1,
            Math.Min(cpu, 12));

        uint readWorkbook;
        uint readPresentation;
        if (diskFactor is { } d)
        {
            // ZIP/XML/object-allocation-heavy; disk helps, but RAM and GC pressure dominates past a point.
            readWorkbook = Clamp((1.2 + d * 0.45) * Math.Sqrt(cpu) * ramFactor, 1, 6);
            // Heavier than workbook due to images/media/layout graph.
            readPresentation = Clamp((1.0 + d * 0.35) * Math.Sqrt(cpu) * ramFactor, 1, 5);
        }
        else
        {
            readWorkbook = current.MaxParallelReadWorkbook;
            readPresentation = current.MaxParallelReadPresentation;
        }

        // Serialize/write-heavy; low cap is intentional.
        var editPresentation = Clamp(Math.Sqrt(cpu) * 0.9 * ramFactor, 1, 4);

        #endregion

        return new Setting.PerformanceSetting
        {
            MaxParallelDownloadImage = downloadImage,
            MaxParallelEditImage = editImage,
            MaxParallelReadWorkbook = readWorkbook,
            MaxParallelReadPresentation = readPresentation,
            MaxParallelEditPresentation = editPresentation
        };
    }

    private static uint Clamp(double value, double min, double max)
    {
        return (uint)Math.Clamp(
            (int)Math.Round(value, MidpointRounding.AwayFromZero),
            (int)min,
            (int)max);
    }
}
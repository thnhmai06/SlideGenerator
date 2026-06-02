/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: SettingProbe.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Diagnostics;
using System.Net;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Settings.Domain.Models;

namespace SlideGenerator.Settings.Infrastructure.Services;

/// <summary>
///     Collects raw measurements from the host environment.
///     Each public method probes a specific resource category and returns the results
///     for use by <see cref="Application.Services.SettingTuner" />.
/// </summary>
internal static class SettingProbe
{
    /// <summary>Number of concurrent streams used to measure aggregate pipe bandwidth.</summary>
    public const int ParallelStreamCount = 8;

    private const string BandwidthEndpoint = "https://speed.cloudflare.com/__down?bytes=5000000"; // 5MB in Networks

    private static readonly string[] LatencyEndpoints =
    [
        "https://www.googleapis.com/",
        "https://graph.microsoft.com/"
    ];

    /// <summary>
    ///     Probes hardware (CPU, RAM, disk) and network (bandwidth, latency) conditions,
    ///     returning raw measurements for use by <see cref="Application.Services.SettingTuner.TunePerformance" />.
    ///     All network requests respect the proxy configuration in <see cref="Setting.NetworkSetting" />.
    /// </summary>
    public static async Task<ProbeResult> ProbePerformanceAsync(
        Setting.NetworkSetting network,
        CancellationToken ct = default)
    {
        var cpuCount = Environment.ProcessorCount;
        var ramGiB = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024);

        var diskMBps = await MeasureDiskAsync(ct).ConfigureAwait(false);
        var (networkMbps, singleStreamMbps, latencyMs) = await MeasureNetworkAsync(network, ct).ConfigureAwait(false);

        return new ProbeResult(cpuCount, ramGiB, diskMBps, networkMbps, latencyMs, singleStreamMbps);
    }

    #region Disk

    /// <summary>
    ///     Measures local disk write performance by writing a temporary buffer to the file system.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Disk throughput in MB/s, or <see langword="null" /> if the measurement fails.</returns>
    private static async Task<double?> MeasureDiskAsync(CancellationToken ct)
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var buf = new byte[10 * 1024 * 1024]; // 10MB
            Random.Shared.NextBytes(buf);
            var sw = Stopwatch.StartNew();
            await File.WriteAllBytesAsync(tmp, buf, ct).ConfigureAwait(false);
            sw.Stop();
            return buf.Length / (1024.0 * 1024) / sw.Elapsed.TotalSeconds;
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                File.Delete(tmp);
            }
            catch
            {
                /* ignore cleanup failure */
            }
        }
    }

    #endregion

    #region Network

    /// <summary>
    ///     Performs a sequence of network probes including warm-up, bandwidth testing, and latency measurement.
    /// </summary>
    /// <param name="network">The network settings containing proxy and timeout configurations.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing aggregate bandwidth, single-stream bandwidth, and median latency.</returns>
    private static async Task<(double? NetworkMbps, double? SingleStreamMbps, double? LatencyMs)> MeasureNetworkAsync(
        Setting.NetworkSetting network, CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(network.Retry.Timeout);
        var handler = BuildHandler(network.Proxy.GetWebProxy());
        using var client = new HttpClient(handler);
        client.Timeout = timeout;

        // Warm-up: absorb TLS handshake and TCP slow-start before timing measurements.
        await WarmUpAsync(client, ct).ConfigureAwait(false);

        var (networkMbps, singleStreamMbps) = await MeasureBandwidthAsync(client, ct).ConfigureAwait(false);
        var latencyMs = await MeasureLatencyAsync(client, ct).ConfigureAwait(false);

        return (networkMbps, singleStreamMbps, latencyMs);
    }

    /// <summary>
    ///     Constructs an <see cref="HttpClientHandler" /> with the specified proxy configuration.
    /// </summary>
    /// <param name="proxy">The web proxy to use, if any.</param>
    /// <returns>A configured handler.</returns>
    private static HttpClientHandler BuildHandler(IWebProxy? proxy)
    {
        var handler = new HttpClientHandler();
        if (proxy != null)
        {
            handler.UseProxy = true;
            handler.Proxy = proxy;
        }

        return handler;
    }

    /// <summary>
    ///     Performs a best-effort request to warm up the connection (TLS/TCP) before measurements start.
    /// </summary>
    /// <param name="client">The HTTP client instance.</param>
    /// <param name="ct">The cancellation token.</param>
    private static async Task WarmUpAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            await client.GetByteArrayAsync(BandwidthEndpoint, ct).ConfigureAwait(false);
        }
        catch
        {
            /* ignore — warm-up is best-effort */
        }
    }

    /// <summary>
    ///     Measures both per-connection throughput (<em>r</em>) and aggregate pipe bandwidth (<em>B</em>).
    ///     Returns <see langword="null" /> for each component when the corresponding request fails.
    /// </summary>
    /// <summary>
    ///     Measures both per-connection throughput (r) and aggregate pipe bandwidth (B).
    ///     Returns <see langword="null" /> for each component when the corresponding request fails.
    /// </summary>
    /// <param name="client">The HTTP client instance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing aggregate and single-stream throughput in Mbps.</returns>
    private static async Task<(double? AggregateMbps, double? SingleMbps)> MeasureBandwidthAsync(
        HttpClient client, CancellationToken ct)
    {
        // Single stream → r
        double? singleMbps;
        try
        {
            var sw = Stopwatch.StartNew();
            var data = await client.GetByteArrayAsync(BandwidthEndpoint, ct).ConfigureAwait(false);
            sw.Stop();
            singleMbps = data.Length * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000;
        }
        catch
        {
            singleMbps = null;
        }

        // Parallel streams → aggregate B; null if all streams failed
        double? aggregateMbps;
        try
        {
            var sw = Stopwatch.StartNew();
            var tasks = Enumerable
                .Range(0, ParallelStreamCount)
                .Select(_ => TrySingleDownloadAsync(client, ct))
                .ToArray();
            var lengths = await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();
            var totalBytes = lengths.Sum();
            aggregateMbps = totalBytes > 0
                ? totalBytes * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000
                : null;
        }
        catch
        {
            aggregateMbps = null;
        }

        return (aggregateMbps, singleMbps);
    }

    /// <summary>
    ///     Attempts a single download and returns the number of bytes received.
    /// </summary>
    /// <param name="client">The HTTP client instance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of bytes downloaded, or 0 if the request fails.</returns>
    private static async Task<long> TrySingleDownloadAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            var data = await client.GetByteArrayAsync(BandwidthEndpoint, ct).ConfigureAwait(false);
            return data.Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    ///     Measures latency by sampling each endpoint multiple times and returning the median.
    ///     Median is more robust than mean against redirect spikes on first cold requests.
    /// </summary>
    /// <param name="client">The HTTP client instance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The median latency in milliseconds, or <see langword="null" /> if all probes fail.</returns>
    private static async Task<double?> MeasureLatencyAsync(HttpClient client, CancellationToken ct)
    {
        const int samplesPerEndpoint = 5;
        var samples = new List<double>(samplesPerEndpoint * LatencyEndpoints.Length);

        foreach (var endpoint in LatencyEndpoints)
            for (var i = 0; i < samplesPerEndpoint; i++)
                try
                {
                    var sw = Stopwatch.StartNew();
                    using var response = await client
                        .GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, ct)
                        .ConfigureAwait(false);
                    sw.Stop();
                    samples.Add(sw.Elapsed.TotalMilliseconds);
                }
                catch
                {
                    /* skip unreachable endpoints */
                }

        if (samples.Count == 0) return null;

        samples.Sort();
        return samples[samples.Count / 2]; // median
    }

    #endregion
}
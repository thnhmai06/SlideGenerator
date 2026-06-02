# Settings Module

The **SlideGenerator.Settings** module handles application-wide configuration.

## Responsibility

- Persisting user preferences to YAML.
- Providing reactive updates to other modules via `ISettingProvider`.
- Auto-calibrating performance gate limits from live hardware and network measurements.

## Persistence

- **Format**: YAML (via YamlDotNet).
- **Location**: `%LOCALAPPDATA%/SlideGenerator/settings.yaml`.
- **Reset**: Includes a default configuration provider to restore factory settings.

## Configuration Categories

| Category | Description |
|----------|-------------|
| **Performance** | Gate parallelism limits for pipeline stages. |
| **Network** | Proxy settings and retry/timeout policy. Proxy passwords are AES-256 encrypted via the Cryptography module. |
| **Logging** | Minimum log levels and retention policies. |
| **App** | Visual preferences and default export paths. |

## Performance Calibration

`ISettingCalibrator.CalibratePerformanceAsync()` probes hardware and network, then derives recommended
`PerformanceSetting` gate limits using the formulas below.
The result is a `PerformanceCalibration` record; the caller applies it via `ISettingManager.Update()`.

### Probe (`SettingProbe.ProbePerformanceAsync`)

| Measurement | Method | Fallback |
|-------------|--------|----------|
| CPU logical cores | `Environment.ProcessorCount` | — |
| Available RAM (GiB) | `GC.GetGCMemoryInfo().TotalAvailableMemoryBytes` | — |
| Disk write speed (MB/s) | Sequential 10 MB temp-file write | 100 MB/s |
| Single-stream bandwidth $r$ (Mbps) | Single HTTP GET from Cloudflare speed endpoint (post warm-up) | 12 Mbps |
| Aggregate bandwidth $B$ (Mbps) | 8 parallel HTTP GETs from the same endpoint | $= r$ |
| Latency $L$ (ms) | Median of 5 RTT samples per endpoint (googleapis, graph.microsoft.com) | 200 ms |

A **warm-up GET** is issued before timing to absorb TLS handshake and TCP slow-start distortion.

### Tuner (`SettingTuner.TunePerformance`)

#### Shared factors

RAM headroom factor — clipped to avoid extremes on very low or very high memory machines:

$$r_{\text{RAM}} = \text{clamp}\!\left(\frac{\text{RAM}_{\text{GiB}}}{16},\; 0.6,\; 1.5\right)$$

Disk throughput factor — log-scaled so a fast NVMe cannot dominate over other terms:

$$d = \text{clamp}\!\left(\log_2\!\!\left(1 + \frac{D_{\text{MB/s}}}{250}\right),\; 0.5,\; 2.5\right)$$

#### Download gate — network-bound

Derived from the **bandwidth-delay model** for fetching many small files over an independently
throttled cloud storage link.  Google Drive caps each TCP stream at $r \approx 12\ \text{Mbps}$, so
saturating a faster pipe requires opening $B/r$ concurrent connections:

$$N_{\text{download}} = \text{clamp}\!\left(2 + \frac{B}{r} + \frac{B \cdot L}{S},\; 2,\; 32\right)$$

| Symbol | Value / Source | Description |
|--------|----------------|-------------|
| $B$ | `ProbeResult.NetworkMbps` | Aggregate pipe bandwidth, from 8-stream parallel probe |
| $r$ | `ProbeResult.SingleStreamMbps` or $r_{\text{assumed}} = 12\ \text{Mbps}$ | Per-connection throughput cap; fallback assumes Drive throttle |
| $L$ | `ProbeResult.LatencyMs` / 1000 | Round-trip latency (seconds) |
| $S$ | $S_{\text{Mbit}} = 8\ \text{Mbit}$ (~1 MB) | Assumed average image file size |

The $B/r$ term is the **primary lever** for Drive: each stream is independently throttled, so
parallelism directly trades connection count for throughput.
The $B \cdot L / S$ term adds extra slots to hide round-trip overhead when files are small relative to latency.

> **Note on constants**: $r_{\text{assumed}}$ and $S_{\text{Mbit}}$ are calibration constants
> defined in `SettingTuner`. Adjust them if empirical Drive measurements on your network differ.

#### CPU-bound gates

Native-thread-heavy operations (OpenCV, ImageMagick) scale with $\sqrt{cpu}$ to avoid
oversubscribing native thread pools:

$$N_{\text{editImage}} = \text{clamp}\!\left(2\sqrt{cpu}\cdot r_{\text{RAM}},\; 1,\; \min(cpu,\;12)\right)$$

ZIP/XML parse is disk- and GC-pressure-sensitive:

$$N_{\text{readWorkbook}} = \text{clamp}\!\left((1.2 + 0.45\,d)\sqrt{cpu}\cdot r_{\text{RAM}},\; 1,\; 6\right)$$

$$N_{\text{readPresentation}} = \text{clamp}\!\left((1.0 + 0.35\,d)\sqrt{cpu}\cdot r_{\text{RAM}},\; 1,\; 5\right)$$

Serialize/write has a deliberately low cap to avoid lock contention on the output file:

$$N_{\text{editPresentation}} = \text{clamp}\!\left(0.9\sqrt{cpu}\cdot r_{\text{RAM}},\; 1,\; 4\right)$$

### Reference values

| Link scenario | $B$ | $r$ | $L$ | $N_{\text{download}}$ |
|---|---|---|---|---|
| Corporate 20 Mbps / 200 ms | 20 Mbps | 12 Mbps | 200 ms | 4 |
| Home 100 Mbps / 80 ms | 100 Mbps | 12 Mbps | 80 ms | 10 |
| Gigabit / 10 ms | 1000 Mbps | 12 Mbps | 10 ms | 32 (capped) |
| Offline / probe failed | 0 Mbps | — | — | 2 (baseline) |

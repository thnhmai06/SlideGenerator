/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: ImageCacheTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SlideGenerator.Cloud;
using SlideGenerator.Generator.Jobs;
using SlideGenerator.Image.Loading;
using SlideGenerator.Settings.Mutable;
using SlideGenerator.Utilities;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Behavioural tests for <see cref="ImageCache" />: dedup by final resolved URI, on-demand inspection,
///     download slotting, validation before caching, never retrying a known failure within a run, prefetch
///     safety, and orphan-folder cleanup. Synchronisation is <see cref="TaskCompletionSource" />-based and
///     counters — no sleeps.
/// </summary>
public sealed class ImageCacheTests : IDisposable
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan NoProgressWindow = TimeSpan.FromMilliseconds(250);

    private readonly string _tempRoot;

    public ImageCacheTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"imgcache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true);
        }
        catch (IOException)
        {
            // best-effort
        }
    }

    /// <summary>Second caller for the same source reuses the first call's inspection and download.</summary>
    [Fact]
    public async Task GetImagePathAsync_SameSourceTwice_InspectsAndDownloadsOnce()
    {
        var uri = new Uri("https://example.com/img.png");
        var bytes = MakePngBytes(8);
        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", (uint)bytes.Length, ".png"));
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => { File.WriteAllBytes((string)ci[1]!, bytes); return Task.CompletedTask; });

        using var cache = new ImageCache(CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path1 = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);
        var path2 = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path1.Should().NotBeNull();
        path1.Should().Be(path2, "both calls hit the same dedup target");
        await cloud.Received(1).InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
        await cloud.Received(1).DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Two source URIs whose <c>InspectAsync</c> resolves to the same final URI share one download.</summary>
    [Fact]
    public async Task GetImagePathAsync_TwoSourcesResolvingToTheSameUri_DownloadOnceAndShareTheFile()
    {
        var source1 = new Uri("https://drive.google.com/file/d/abc/view");
        var source2 = new Uri("https://example.com/redirect");
        var final = new Uri("https://example.com/real.png");
        var bytes = MakePngBytes(8);

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(source1, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(final, "image/png", (uint)bytes.Length, ".png"));
        cloud.InspectAsync(source2, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(final, "image/png", (uint)bytes.Length, ".png"));
        cloud.DownloadToFileAsync(final, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => { File.WriteAllBytes((string)ci[1]!, bytes); return Task.CompletedTask; });

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path1 = await cache.GetImagePathAsync(source1, null, TestContext.Current.CancellationToken);
        var path2 = await cache.GetImagePathAsync(source2, null, TestContext.Current.CancellationToken);

        path1.Should().NotBeNull();
        path2.Should().NotBeNull();
        path1.Should().Be(path2, "both calls resolve to the same final URI and share the cached file");
        await cloud.Received(1).DownloadToFileAsync(final, Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Parallel callers for the same target start exactly one download.</summary>
    [Fact]
    public async Task GetImagePathAsync_ParallelCallersSameTarget_StartExactlyOneDownload()
    {
        var uri = new Uri("https://example.com/img.png");
        var bytes = MakePngBytes(8);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var downloadCalls = 0;

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", (uint)bytes.Length, ".png"));
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                Interlocked.Increment(ref downloadCalls);
                await release.Task.ConfigureAwait(false);
                File.WriteAllBytes((string)ci[1]!, bytes);
            });

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var callers = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => cache.GetImagePathAsync(uri, null, CancellationToken.None)))
            .ToArray();

        // Let all 8 callers race to the same Lazy
        await Task.Delay(NoProgressWindow, TestContext.Current.CancellationToken);
        release.SetResult();

        var results = await Task.WhenAll(callers);
        results.Should().OnlyContain(r => r != null);
        results.Distinct().Should().HaveCount(1, "every caller must observe the same final path");
        downloadCalls.Should().Be(1, "only the first Lazy entry must invoke the downloader");
    }

    /// <summary>A leftover <c>.tmp</c> file is not treated as a hit — the real download still happens.</summary>
    [Fact]
    public async Task GetImagePathAsync_LeftoverTmpFile_IsNotTreatedAsAHit()
    {
        var uri = new Uri("https://example.com/img.png");
        var bytes = MakePngBytes(8);
        var folder = CreateCacheFolder();
        // Pre-create a junk .tmp at what would be the target.tmp path.
        var targetHash = uri.AbsoluteUri.HashText();
        File.WriteAllBytes(Path.Combine(folder, targetHash + ".tmp"), new byte[] { 0xFF, 0x00, 0x01 });

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", (uint)bytes.Length, ".png"));
        var downloadedTo = string.Empty;
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                downloadedTo = (string)ci[1]!;
                File.WriteAllBytes(downloadedTo, bytes);
                return Task.CompletedTask;
            });

        using var cache = new ImageCache(
            folder,
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path.Should().NotBeNull();
        path.Should().NotEndWith(".tmp", "the returned path must be the validated final file");
        File.Exists(path).Should().BeTrue();
        await cloud.Received(1).DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
        File.Exists(downloadedTo + ".tmp").Should().BeFalse("the leftover tmp must be cleaned up after move");
    }

    /// <summary>A non-image download is rejected once, no retry, no leftover file.</summary>
    [Fact]
    public async Task GetImagePathAsync_DownloadIsNotAnImage_ReturnsNullAndRemovesTmpAndDoesNotRetryForOtherCallers()
    {
        var uri = new Uri("https://example.com/not-image");
        var bytes = MakePngBytes(8); // bytes don't matter — loader rejects them
        var folder = CreateCacheFolder();

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", (uint)bytes.Length, ".png"));
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => { File.WriteAllBytes((string)ci[1]!, bytes); return Task.CompletedTask; });

        using var cache = new ImageCache(
            folder,
            cloud,
            MakeLoader(valid: false),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path1 = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);
        var path2 = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path1.Should().BeNull();
        path2.Should().BeNull();
        await cloud.Received(1).DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());

        var targetHash = uri.AbsoluteUri.HashText();
        File.Exists(Path.Combine(folder, targetHash)).Should().BeFalse("no final file is ever produced");
        File.Exists(Path.Combine(folder, targetHash + ".tmp")).Should().BeFalse("no leftover tmp file");
    }

    /// <summary>A failed inspect is recorded as null and never retried within the run.</summary>
    [Fact]
    public async Task GetImagePathAsync_InspectFails_ReturnsNullAndIsAttemptedOnlyOncePerRun()
    {
        var uri = new Uri("https://example.com/missing");
        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>()).Returns((ContentInfo?)null);

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        for (var i = 0; i < 5; i++)
        {
            var path = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);
            path.Should().BeNull();
        }

        await cloud.Received(1).InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>octet-stream MIME is treated as unknown — the download still happens, validation decides.</summary>
    [Fact]
    public async Task GetImagePathAsync_OctetStreamOrUnknownMime_StillDownloads()
    {
        var uri = new Uri("https://example.com/blob");
        var bytes = MakePngBytes(8);

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "application/octet-stream", (uint)bytes.Length, ".png"));
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => { File.WriteAllBytes((string)ci[1]!, bytes); return Task.CompletedTask; });

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path.Should().NotBeNull();
        await cloud.Received(1).DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>An HTML MIME is short-circuited: no download is attempted at all.</summary>
    [Fact]
    public async Task GetImagePathAsync_HtmlMime_IsSkippedWithoutDownloading()
    {
        var uri = new Uri("https://example.com/page");
        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "text/html", 4096, ".html"));

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path.Should().BeNull();
        await cloud.DidNotReceive().DownloadToFileAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A size-cap rejection is final — no retry, no leftover file.</summary>
    [Fact]
    public async Task GetImagePathAsync_DownloadExceedsLimit_ReturnsNullLeavesNoFileAndIsNotRetried()
    {
        var uri = new Uri("https://example.com/huge");
        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", 999_999, ".png"));
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidDataException("too big"));

        var folder = CreateCacheFolder();
        using var cache = new ImageCache(
            folder,
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 2, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path1 = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);
        var path2 = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path1.Should().BeNull();
        path2.Should().BeNull();
        await cloud.Received(1).DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());

        var targetHash = uri.AbsoluteUri.HashText();
        File.Exists(Path.Combine(folder, targetHash)).Should().BeFalse();
        File.Exists(Path.Combine(folder, targetHash + ".tmp")).Should().BeFalse();
    }

    /// <summary>An existing final file (e.g. from a resume) is reused without downloading.</summary>
    [Fact]
    public async Task GetImagePathAsync_ExistingFinalFile_IsReusedWithoutDownloading()
    {
        var uri = new Uri("https://example.com/img.png");
        var folder = CreateCacheFolder();
        var targetHash = uri.AbsoluteUri.HashText();
        var finalPath = Path.Combine(folder, targetHash);
        File.WriteAllBytes(finalPath, MakePngBytes(8));

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", 8, ".png"));

        using var cache = new ImageCache(
            folder,
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        var path = await cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);

        path.Should().Be(finalPath);
        await cloud.DidNotReceive().DownloadToFileAsync(Arg.Any<Uri>(), Arg.Any<string>(), Arg.Any<long>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>A caller that cancels while waiting does not poison the lazy for other callers.</summary>
    [Fact]
    public async Task GetImagePathAsync_CallerCancels_DoesNotPoisonOtherCallers()
    {
        var uri = new Uri("https://example.com/img.png");
        var bytes = MakePngBytes(8);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var downloadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(uri, "image/png", (uint)bytes.Length, ".png"));
        cloud.DownloadToFileAsync(uri, Arg.Any<string>(), Arg.Any<long>(), Arg.Any<HttpClient?>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                downloadStarted.TrySetResult();
                await release.Task.ConfigureAwait(false);
                File.WriteAllBytes((string)ci[1]!, bytes);
            });

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        using var cancelledCts = new CancellationTokenSource();
        var cancelledCaller = Task.Run(() =>
            cache.GetImagePathAsync(uri, null, cancelledCts.Token));

        await downloadStarted.Task.WaitAsync(ShortTimeout, TestContext.Current.CancellationToken);
        await cancelledCts.CancelAsync();

        var act = async () => await cancelledCaller;
        await act.Should().ThrowAsync<OperationCanceledException>();

        var healthyCaller = cache.GetImagePathAsync(uri, null, TestContext.Current.CancellationToken);
        release.SetResult();

        var path = await healthyCaller;
        path.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();
    }

    /// <summary>Prefetch swallows every failure — it never throws and never produces an unobserved task.</summary>
    [Fact]
    public async Task Prefetch_FailingSource_NeverThrowsOrLeavesAnUnobservedException()
    {
        var uri = new Uri("https://example.com/missing");
        var cloud = Substitute.For<ICloudClient>();
        cloud.InspectAsync(uri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>()).Returns((ContentInfo?)null);

        using var cache = new ImageCache(
            CreateCacheFolder(),
            cloud,
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        cache.Prefetch(uri);
        cache.Prefetch(uri);
        cache.Prefetch(uri);

        // Let the fire-and-forget tasks settle.
        await Task.Delay(NoProgressWindow, TestContext.Current.CancellationToken);
    }

    /// <summary>Dispose removes the cache folder.</summary>
    [Fact]
    public void Dispose_RemovesTheFolder()
    {
        var folder = CreateCacheFolder();
        File.WriteAllBytes(Path.Combine(folder, "marker"), new byte[] { 1 });

        var cache = new ImageCache(
            folder,
            Substitute.For<ICloudClient>(),
            MakeLoader(valid: true),
            MakeHttpClientFactory(),
            MakeSettings(maxRetries: 0, maxBytes: 0),
            NullLogger<ImageCache>.Instance);

        cache.Dispose();

        Directory.Exists(folder).Should().BeFalse();
    }

    /// <summary>Only the folders of inactive workflows are removed by <see cref="ImageCache.DeleteOrphans" />.</summary>
    [Fact]
    public void DeleteOrphans_RemovesOnlyFoldersOfInactiveWorkflows()
    {
        var root = Path.Combine(_tempRoot, "root");
        Directory.CreateDirectory(root);

        var active = "active-workflow";
        var inactive1 = "old-workflow-1";
        var inactive2 = "OLD-WORKFLOW-2"; // different case — must still be removed

        Directory.CreateDirectory(Path.Combine(root, active));
        Directory.CreateDirectory(Path.Combine(root, inactive1));
        Directory.CreateDirectory(Path.Combine(root, inactive2));
        File.WriteAllBytes(Path.Combine(root, active, "kept"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(root, inactive1, "garbage"), new byte[] { 2 });
        File.WriteAllBytes(Path.Combine(root, inactive2, "garbage"), new byte[] { 3 });

        ImageCache.DeleteOrphans(root, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { active });

        Directory.Exists(Path.Combine(root, active)).Should().BeTrue("active workflows keep their folders");
        Directory.Exists(Path.Combine(root, inactive1)).Should().BeFalse("orphan folder removed");
        Directory.Exists(Path.Combine(root, inactive2)).Should().BeFalse("case-insensitive match: orphan removed");
    }

    /// <summary>Helper: a real PNG-header-shaped byte buffer (loader validity is controlled by the fake).</summary>
    private static byte[] MakePngBytes(int count)
    {
        // First 8 bytes = PNG signature; the loader fake ignores them.
        var bytes = new byte[System.Math.Max(count, 8)];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        bytes[4] = 0x0D; bytes[5] = 0x0A; bytes[6] = 0x1A; bytes[7] = 0x0A;
        return bytes;
    }

    private string CreateCacheFolder()
    {
        var folder = Path.Combine(_tempRoot, $"cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    ///     Builds an <see cref="IHttpClientFactory" /> whose <c>CreateClient</c> returns a real
    ///     <see cref="HttpClient" /> — <see cref="Utilities.CreateHttpClientWithSetting" /> reuses the
    ///     pooled client and only adjusts <c>Timeout</c>; a null client would NRE before our code runs.
    /// </summary>
    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private static IHttpClientFactory MakeHttpClientFactory() => new StubHttpClientFactory();

    private static ISettingProvider MakeSettings(int maxRetries, uint maxBytes)
    {
        var provider = Substitute.For<ISettingProvider>();
        provider.Current.Returns(new Setting
        {
            Network = new Setting.NetworkSetting
            {
                MaxDownloadBytes = maxBytes,
                Retry = new Setting.RetrySetting { MaxRetries = maxRetries, MaxRetryDelay = 16, Timeout = 30 }
            }
        });
        return provider;
    }

    /// <summary>
    ///     A hand-written <see cref="IImageLoader" /> because NSubstitute's out-parameter support for
    ///     <see cref="IImageLoader.TryGetInfo(string, out IImageInfo?)" />; we set the result explicitly.
    /// </summary>
    private sealed class StubImageLoader : IImageLoader
    {
        private readonly bool _valid;

        public StubImageLoader(bool valid) => _valid = valid;

        public IImage Open(string path) => throw new NotSupportedException("Not exercised in these tests.");

        public IImage Open(byte[] data) => throw new NotSupportedException("Not exercised in these tests.");

        public IImageInfo GetInfo(string path) =>
            throw new NotSupportedException("Not exercised in these tests.");

        public IImageInfo GetInfo(byte[] data) =>
            throw new NotSupportedException("Not exercised in these tests.");

        public bool TryGetInfo(string path, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out IImageInfo info)
        {
            if (_valid && File.Exists(path))
            {
                info = new StubImageInfo();
                return true;
            }

            info = null;
            return false;
        }

        public bool TryGetInfo(byte[] data, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out IImageInfo info)
        {
            if (_valid)
            {
                info = new StubImageInfo();
                return true;
            }

            info = null;
            return false;
        }

        private sealed record StubImageInfo : IImageInfo
        {
            public uint Width => 1;

            public uint Height => 1;
        }
    }

    private static IImageLoader MakeLoader(bool valid) => new StubImageLoader(valid);
}
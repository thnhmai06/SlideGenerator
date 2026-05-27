/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: CollectImageTests.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using NSubstitute;
using SlideGenerator.Cloud.Application.Abstractions;
using SlideGenerator.Cloud.Domain.Models;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Generator.Tests.Unit.Helpers;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Summarization.Domain.Models.Recipes;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="CollectImage" />, covering the early-exit paths and the
///     three-step URL resolution pipeline (inspect → cloud-resolve → inspect → download).
///     All I/O is intercepted by NSubstitute fakes.
/// </summary>
public sealed class CollectImageTests
{
    private readonly ICloudClient _cloudClient = Substitute.For<ICloudClient>();
    private readonly ICloudResolver _cloudResolver = Substitute.For<ICloudResolver>();
    private readonly IGateLocker _gateLocker = Substitute.For<IGateLocker>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IImageFactory _imageFactory = Substitute.For<IImageFactory>();

    /// <summary>Configures stubs shared across all tests.</summary>
    public CollectImageTests()
    {
        _gateLocker.AcquireAsync(Arg.Any<GateType>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        _httpClientFactory.CreateClient().Returns(new HttpClient());
    }

    #region Step 1 — first InspectAsync fails

    /// <summary>
    ///     Verifies that <see cref="CollectImage.RunAsync" /> logs a warning and skips the download
    ///     when the initial <see cref="ICloudClient.InspectAsync" /> call returns <see langword="null" />
    ///     (network error or unreachable host).
    /// </summary>
    [Fact]
    public async Task RunAsync_FirstInspectReturnsNull_LogsWarningAndSkipsDownload()
    {
        const string url = "https://example.com/image.jpg";
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = BuildPrimaryCoordinator(url);

        // InspectAsync returns null by default (NSubstitute default for Task<T?> → null)
        var step = BuildStep(BuildImageContext(url));

        await step.RunAsync(ctx);

        await _cloudClient.DidNotReceive().DownloadAsync(
            Arg.Any<Uri>(), Arg.Any<string>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
        data.Logger!.Received().Warning(Arg.Any<string>(), Arg.Any<object?[]>());
    }

    #endregion

    #region Helpers

    private static ImageContext BuildImageContext(string? sourceUrl)
    {
        var sheetId = new SheetIdentifier(
            Path.Combine(Path.GetTempPath(), "workbook.xlsx"), "Sheet1");
        return new ImageContext(
            sheetId,
            1,
            "Photo",
            "ImageShape",
            sourceUrl,
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg"),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg"),
            100,
            100,
            new EditOptions(new CenterOption { UseFaceAlignment = false }));
    }

    private CollectImage BuildStep(ImageContext imageCtx)
    {
        return new CollectImage(_cloudClient, _cloudResolver, _imageFactory, _gateLocker, _httpClientFactory)
        {
            Task = imageCtx
        };
    }

    /// <summary>
    ///     Creates a coordinator stub whose <c>Enlist</c> call for <paramref name="url" />
    ///     returns a <see cref="PrimaryEnlistment" /> with no-op callbacks.
    /// </summary>
    private static ICoordinator BuildPrimaryCoordinator(string url)
    {
        var coordinator = Substitute.For<ICoordinator>();
        coordinator.Enlist(url).Returns(new PrimaryEnlistment(_ => { }, _ => { }));
        return coordinator;
    }

    #endregion

    #region Null URL — early exit before pipeline

    /// <summary>
    ///     Verifies that <see cref="CollectImage.RunAsync" /> returns immediately without invoking
    ///     <see cref="ICloudClient.DownloadAsync" /> when <see cref="ImageContext.SourceUrl" /> is
    ///     <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task RunAsync_NullUrl_DoesNotCallDownload()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = Substitute.For<ICoordinator>();
        var step = BuildStep(BuildImageContext(null));

        await step.RunAsync(ctx);

        await _cloudClient.DidNotReceive().DownloadAsync(
            Arg.Any<Uri>(), Arg.Any<string>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Verifies that <see cref="CollectImage.RunAsync" /> logs a warning when
    ///     <see cref="ImageContext.SourceUrl" /> is <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task RunAsync_NullUrl_LogsWarning()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = Substitute.For<ICoordinator>();
        var step = BuildStep(BuildImageContext(null));

        await step.RunAsync(ctx);

        data.Logger!.Received().Warning(Arg.Any<string>());
    }

    #endregion

    #region Non-cloud URL pipeline

    /// <summary>
    ///     Verifies that when the URL is not a cloud link and the final inspect returns a non-image
    ///     content-type, <see cref="CollectImage.RunAsync" /> skips the download.
    /// </summary>
    [Fact]
    public async Task RunAsync_NonCloudUrl_FinalInspectNonImage_SkipsDownload()
    {
        const string url = "https://example.com/document.pdf";
        var finalUri = new Uri(url);
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = BuildPrimaryCoordinator(url);

        // Both inspect calls return a non-image ContentInfo
        _cloudClient.InspectAsync(Arg.Any<Uri>(), Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(finalUri, "application/pdf", null));
        // GetCloudHost returns false by default (NSubstitute bool default)

        var step = BuildStep(BuildImageContext(url));
        await step.RunAsync(ctx);

        await _cloudClient.DidNotReceive().DownloadAsync(
            Arg.Any<Uri>(), Arg.Any<string>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Verifies that when the URL is not a cloud link and the final inspect returns an image
    ///     content-type, <see cref="CollectImage.RunAsync" /> calls
    ///     <see cref="ICloudClient.DownloadAsync" /> exactly once.
    /// </summary>
    [Fact]
    public async Task RunAsync_NonCloudUrl_FinalInspectIsImage_CallsDownload()
    {
        const string url = "https://cdn.example.com/photo.jpg";
        var finalUri = new Uri(url);
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = BuildPrimaryCoordinator(url);

        _cloudClient.InspectAsync(Arg.Any<Uri>(), Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(finalUri, "image/jpeg", 102_400u));
        // GetCloudHost returns false by default

        var step = BuildStep(BuildImageContext(url));
        await step.RunAsync(ctx);

        await _cloudClient.Received(1).DownloadAsync(
            Arg.Any<Uri>(), Arg.Any<string>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cloud URL pipeline

    /// <summary>
    ///     Verifies that when the URL is a cloud link and <see cref="ICloudResolver.ResolveAsync" />
    ///     returns <see langword="null" /> (e.g. permission denied), the download is skipped.
    /// </summary>
    [Fact]
    public async Task RunAsync_CloudUrl_ResolveReturnsNull_SkipsDownload()
    {
        const string url = "https://drive.google.com/file/d/RESTRICTED/view";
        var driveUri = new Uri(url);
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = BuildPrimaryCoordinator(url);

        _cloudClient.InspectAsync(Arg.Any<Uri>(), Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(driveUri, null, null));

        CloudHost dummyKey;
        _cloudResolver.GetCloudHost(Arg.Any<string>(), out dummyKey).Returns(true);
        _cloudResolver.ResolveAsync(Arg.Any<string>(), Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns((Uri?)null);

        var step = BuildStep(BuildImageContext(url));
        await step.RunAsync(ctx);

        await _cloudClient.DidNotReceive().DownloadAsync(
            Arg.Any<Uri>(), Arg.Any<string>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Verifies the full happy-path for a cloud URL: <see cref="ICloudResolver.ResolveAsync" />
    ///     returns a direct download URI, the final inspect confirms it is an image, and
    ///     <see cref="ICloudClient.DownloadAsync" /> is called exactly once.
    /// </summary>
    [Fact]
    public async Task RunAsync_CloudUrl_ResolvesAndInspectIsImage_CallsDownload()
    {
        const string url = "https://drive.google.com/file/d/ABC123/view";
        var driveUri = new Uri(url);
        var downloadUri = new Uri("https://drive.google.com/uc?export=download&id=ABC123");
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = BuildPrimaryCoordinator(url);

        // Step 1: inspect follows redirect to same URI (no actual redirect in test)
        _cloudClient.InspectAsync(driveUri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(driveUri, null, null));

        // Step 2: cloud resolve
        CloudHost dummyKey;
        _cloudResolver.GetCloudHost(Arg.Any<string>(), out dummyKey).Returns(true);
        _cloudResolver.ResolveAsync(Arg.Any<string>(), Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(downloadUri);

        // Step 3: inspect resolved URI → image
        _cloudClient.InspectAsync(downloadUri, Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>())
            .Returns(new ContentInfo(downloadUri, "image/jpeg", 204_800u));

        var step = BuildStep(BuildImageContext(url));
        await step.RunAsync(ctx);

        await _cloudClient.Received(1).DownloadAsync(
            downloadUri, Arg.Any<string>(),
            Arg.Any<HttpClient?>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
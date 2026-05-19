/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: DownloadImageTests.cs
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
using SlideGenerator.Acquisition.Application.Abstractions;
using SlideGenerator.Acquisition.Domain.Models;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Document.Domain.Models.Sheet;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Domain.Models.Contexts;
using SlideGenerator.Generator.Tests.Unit.Helpers;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Application.Models;
using SlideGenerator.Settings.Application.Abstractions;
using SlideGenerator.Settings.Domain.Entities;
using SlideGenerator.Summarization.Domain.Models.Recipes;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="AcquireImage" />, covering the early-exit paths that fire
///     before any network or file-system I/O occurs.
/// </summary>
public sealed class DownloadImageTests
{
    private readonly IGateLocker _gateLocker = Substitute.For<IGateLocker>();
    private readonly IImageAcquirer _imageAcquirer = Substitute.For<IImageAcquirer>();
    private readonly IImageFactory _imageFactory = Substitute.For<IImageFactory>();
    private readonly ISettingProvider _settingProvider = Substitute.For<ISettingProvider>();

    public DownloadImageTests()
    {
        _settingProvider.Current.Returns(new Setting());
    }

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
            Path.Combine(Path.GetTempPath(), "download.jpg"),
            Path.Combine(Path.GetTempPath(), "edit.jpg"),
            100,
            100,
            new EditOptions(new CenterOption { UseFaceAlignment = false }));
    }

    #endregion

    #region Null URL — immediate return

    /// <summary>
    ///     Verifies that <see cref="AcquireImage.RunAsync" /> returns immediately without calling
    ///     <see cref="IImageAcquirer.AcquireAsync" /> when <see cref="ImageContext.SourceUrl" /> is
    ///     <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task RunAsync_NullUrl_DoesNotCallImageAcquirer()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = Substitute.For<ICoordinator>();

        var imageCtx = BuildImageContext(null);
        var step = new AcquireImage(_imageAcquirer, _imageFactory, _gateLocker, _settingProvider)
        {
            Task = imageCtx
        };

        await step.RunAsync(ctx);

        await _imageAcquirer.DidNotReceive().AcquireAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DownloadConfiguration>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Verifies that <see cref="AcquireImage.RunAsync" /> logs a warning when
    ///     <see cref="ImageContext.SourceUrl" /> is <see langword="null" />.
    /// </summary>
    [Fact]
    public async Task RunAsync_NullUrl_LogsWarning()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        data.AssetCoordinator = Substitute.For<ICoordinator>();

        var imageCtx = BuildImageContext(null);
        var step = new AcquireImage(_imageAcquirer, _imageFactory, _gateLocker, _settingProvider)
        {
            Task = imageCtx
        };

        await step.RunAsync(ctx);

        data.Logger!.Received().Warning(Arg.Any<string>());
    }

    #endregion
}
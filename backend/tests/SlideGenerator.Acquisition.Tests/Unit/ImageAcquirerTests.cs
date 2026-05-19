/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Acquisition.Tests
 * File: ImageAcquirerTests.cs
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

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Acquisition.Application.Abstractions;
using SlideGenerator.Acquisition.Domain.Models;
using SlideGenerator.Acquisition.Infrastructure.Services;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Acquisition.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="ImageAcquirer" />, verifying local-path detection and URI validation.
///     Network download paths are not covered here — they require integration tests with real HTTP endpoints.
/// </summary>
public sealed class ImageAcquirerTests : IDisposable
{
    private readonly ImageAcquirer _acquirer;
    private readonly ICloudResolver _cloudResolver = Substitute.For<ICloudResolver>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    /// <summary>Initializes the acquirer and a temporary directory for file I/O.</summary>
    public ImageAcquirerTests()
    {
        Directory.CreateDirectory(_tempDir);
        _acquirer = new ImageAcquirer(_cloudResolver, _httpClientFactory, Substitute.For<ISystemLogger>());
    }

    private static DownloadConfiguration DefaultConfig => new();

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_tempDir, true);
    }

    private string TempFile(string name)
    {
        return Path.Combine(_tempDir, name);
    }

    #region Local path — allowLocalPath = true

    /// <summary>
    ///     Verifies that when <paramref name="allowLocalPath" /> is <see langword="true" /> and the URL
    ///     refers to an existing local file, the acquirer hard-links (or copies) it without making any
    ///     HTTP requests.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LocalFileExists_AllowLocalTrue_CreatesDestinationFile()
    {
        var src = TempFile("source.jpg");
        var dst = TempFile("dest.jpg");
        await File.WriteAllBytesAsync(src, [0xFF, 0xD8, 0xFF]);

        await _acquirer.AcquireAsync(src, dst, DefaultConfig, true,
            TestContext.Current.CancellationToken);

        File.Exists(dst).Should().BeTrue();
        _httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    /// <summary>
    ///     Verifies that when <paramref name="allowLocalPath" /> is <see langword="false" /> and the URL
    ///     is a local path, the acquirer does NOT treat it as a local file and instead tries to parse
    ///     it as a URI, throwing <see cref="ArgumentException" /> for an invalid URL.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_LocalFileExists_AllowLocalFalse_ThrowsForInvalidUri()
    {
        var src = TempFile("source.jpg");
        var dst = TempFile("dest.jpg");
        await File.WriteAllBytesAsync(src, [0xFF, 0xD8, 0xFF]);

        // The local path is not a valid HTTP URL so NormalizeUri returns null → ArgumentException
        var act = () => _acquirer.AcquireAsync(src, dst, DefaultConfig, false,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Invalid URL

    /// <summary>
    ///     Verifies that <see cref="ImageAcquirer.AcquireAsync" /> throws <see cref="ArgumentException" />
    ///     when the URL is an empty string (not a local file and not a valid URI).
    /// </summary>
    [Fact]
    public async Task AcquireAsync_EmptyUrl_ThrowsArgumentException()
    {
        var dst = TempFile("dest.jpg");

        var act = () => _acquirer.AcquireAsync(string.Empty, dst, DefaultConfig,
            ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="ImageAcquirer.AcquireAsync" /> throws <see cref="ArgumentException" />
    ///     when the URL is clearly invalid (not parseable as a URI).
    /// </summary>
    [Fact]
    public async Task AcquireAsync_InvalidUrl_ThrowsArgumentException()
    {
        var dst = TempFile("dest.jpg");

        var act = () => _acquirer.AcquireAsync("not-a-uri-at-all!!!", dst, DefaultConfig,
            ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
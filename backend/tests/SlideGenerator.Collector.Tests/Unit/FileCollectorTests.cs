/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector.Tests
 * File: FileCollectorTests.cs
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
using NSubstitute.ExceptionExtensions;
using SlideGenerator.Collector.Domain.Models;
using SlideGenerator.Collector.Infrastructure.Services;
using SlideGenerator.Image.Application.Abstractions;
using SlideGenerator.Image.Domain.Entities;
using SlideGenerator.Logging.Domain.Abstractions;
using Xunit;

namespace SlideGenerator.Collector.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="FileCollector" />, verifying local-path detection, URI validation,
///     and image source checking. Network download paths are not covered here — they require
///     integration tests with real HTTP endpoints.
/// </summary>
public sealed class FileCollectorTests : IDisposable
{
    private readonly FileCollector _collector;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IImageFactory _imageFactory = Substitute.For<IImageFactory>();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    /// <summary>Initializes the collector and a temporary directory for file I/O.</summary>
    public FileCollectorTests()
    {
        Directory.CreateDirectory(_tempDir);
        _collector = new FileCollector(_imageFactory, _httpClientFactory, Substitute.For<ISystemLogger>());
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

    #region Local path

    /// <summary>
    ///     Verifies that when the source refers to an existing local file that is a valid image,
    ///     the collector hard-links (or copies) it without making any HTTP requests.
    /// </summary>
    [Fact]
    public async Task AcquireImageAsync_LocalFileExists_CreatesDestinationFile()
    {
        var src = TempFile("source.jpg");
        var dst = TempFile("dest.jpg");
        await File.WriteAllBytesAsync(src, [0xFF, 0xD8, 0xFF]);
        _imageFactory.Open(src).Returns(Substitute.For<IImage>());

        await _collector.AcquireImageAsync(src, dst, DefaultConfig,
            TestContext.Current.CancellationToken);

        File.Exists(dst).Should().BeTrue();
        _httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    #endregion

    #region Invalid URL

    /// <summary>
    ///     Verifies that <see cref="FileCollector.AcquireImageAsync" /> throws <see cref="ArgumentException" />
    ///     when the source is an empty string (not a local file and not a valid URI).
    /// </summary>
    [Fact]
    public async Task AcquireImageAsync_EmptySource_ThrowsArgumentException()
    {
        var dst = TempFile("dest.jpg");

        var act = () => _collector.AcquireImageAsync(string.Empty, dst, DefaultConfig,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    ///     Verifies that <see cref="FileCollector.AcquireImageAsync" /> throws <see cref="ArgumentException" />
    ///     when the source is clearly invalid (not parseable as a URI).
    /// </summary>
    [Fact]
    public async Task AcquireImageAsync_InvalidSource_ThrowsArgumentException()
    {
        var dst = TempFile("dest.jpg");

        var act = () => _collector.AcquireImageAsync("not-a-uri-at-all!!!", dst, DefaultConfig,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsImageSourceAsync — local file

    /// <summary>
    ///     Verifies that <see cref="FileCollector.IsImageSourceAsync" /> returns <see langword="true" />
    ///     and the resolved source equals the original path when the source is a valid local image.
    /// </summary>
    [Fact]
    public async Task IsImageSourceAsync_LocalFile_ValidImage_ReturnsTrueAndPath()
    {
        var src = TempFile("valid.jpg");
        await File.WriteAllBytesAsync(src, [0xFF, 0xD8, 0xFF]);
        _imageFactory.Open(src).Returns(Substitute.For<IImage>());

        var (isValid, resolvedSource) = await _collector.IsImageSourceAsync(src, TestContext.Current.CancellationToken);

        isValid.Should().BeTrue();
        resolvedSource.Should().Be(src);
    }

    /// <summary>
    ///     Verifies that <see cref="FileCollector.IsImageSourceAsync" /> returns <see langword="false" />
    ///     and a <see langword="null" /> resolved source when the file cannot be opened as an image.
    /// </summary>
    [Fact]
    public async Task IsImageSourceAsync_LocalFile_NotAnImage_ReturnsFalseAndNull()
    {
        var src = TempFile("notimage.bin");
        await File.WriteAllBytesAsync(src, [0x00, 0x01, 0x02]);
        _imageFactory.Open(src).Throws(new Exception("Not an image"));

        var (isValid, resolvedSource) = await _collector.IsImageSourceAsync(src, TestContext.Current.CancellationToken);

        isValid.Should().BeFalse();
        resolvedSource.Should().Be(src);
    }

    #endregion
}

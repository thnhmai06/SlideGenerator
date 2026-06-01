/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: VipsImageLoaderTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NetVips;
using SlideGenerator.Image.Infrastructure.Services;
using Xunit;
using NetVipsImage = NetVips.Image;

namespace SlideGenerator.Image.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="VipsImageLoader" />, verifying image loading, metadata
///     retrieval, and the graceful error handling of <see cref="VipsImageLoader.TryGetInfo" />.
///     Images used by these tests are written to and deleted from the temp directory.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VipsImageLoaderTests : IDisposable
{
    private readonly VipsImageLoader _loader = new();
    private readonly List<string> _tempFiles = [];

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var f in _tempFiles.Where(File.Exists))
            File.Delete(f);
    }

    #region Helpers

    /// <summary>
    ///     Creates a temporary PNG file of the given dimensions and registers it for cleanup.
    /// </summary>
    private string CreateTempPng(int width, int height)
    {
        using var native = NetVipsImage.Black(width, height, 3);
        var path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        native.WriteToFile(path);
        _tempFiles.Add(path);
        return path;
    }

    /// <summary>
    ///     Writes arbitrary bytes to a temp file and registers it for cleanup.
    /// </summary>
    private string CreateCorruptFile()
    {
        var path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
        File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0x03]);
        _tempFiles.Add(path);
        return path;
    }

    #endregion

    #region Open

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.Open" /> loads a valid file and returns an
    ///     image with the correct dimensions.
    /// </summary>
    [Fact]
    public void Open_ExistingPngFile_ReturnsImageWithCorrectDimensions()
    {
        var path = CreateTempPng(100, 80);

        using var image = _loader.Open(path);

        image.Info.Width.Should().Be(100u);
        image.Info.Height.Should().Be(80u);
    }

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.Open" /> throws a <see cref="VipsException" />
    ///     when the file does not exist.
    /// </summary>
    [Fact]
    public void Open_NonExistentFile_ThrowsVipsException()
    {
        var act = () => _loader.Open(Path.Combine(Path.GetTempPath(), "does_not_exist_xyz.png"));

        act.Should().Throw<VipsException>();
    }

    #endregion

    #region GetInfo

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.GetInfo" /> returns the correct dimensions
    ///     for an existing PNG file.
    /// </summary>
    [Fact]
    public void GetInfo_ExistingPngFile_ReturnsCorrectDimensions()
    {
        var path = CreateTempPng(120, 90);

        var info = _loader.GetInfo(path);

        info.Width.Should().Be(120u);
        info.Height.Should().Be(90u);
    }

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.GetInfo" /> throws when the file does not exist.
    /// </summary>
    [Fact]
    public void GetInfo_NonExistentFile_ThrowsVipsException()
    {
        var act = () => _loader.GetInfo(Path.Combine(Path.GetTempPath(), "no_such_file_abc.png"));

        act.Should().Throw<VipsException>();
    }

    #endregion

    #region TryGetInfo

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.TryGetInfo" /> returns <see langword="true" />
    ///     and the correct dimensions for an existing valid file.
    /// </summary>
    [Fact]
    public void TryGetInfo_ExistingPngFile_ReturnsTrueWithCorrectDimensions()
    {
        var path = CreateTempPng(64, 48);

        var success = _loader.TryGetInfo(path, out var info);

        success.Should().BeTrue();
        info.Should().NotBeNull();
        info!.Width.Should().Be(64u);
        info.Height.Should().Be(48u);
    }

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.TryGetInfo" /> returns <see langword="false" />
    ///     and sets info to <see langword="null" /> when the file does not exist.
    /// </summary>
    [Fact]
    public void TryGetInfo_NonExistentFile_ReturnsFalseWithNullInfo()
    {
        var success = _loader.TryGetInfo(
            Path.Combine(Path.GetTempPath(), "nonexistent_xyz.png"), out var info);

        success.Should().BeFalse();
        info.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that <see cref="VipsImageLoader.TryGetInfo" /> returns <see langword="false" />
    ///     and sets info to <see langword="null" /> when the file contains corrupt data.
    /// </summary>
    [Fact]
    public void TryGetInfo_CorruptFile_ReturnsFalseWithNullInfo()
    {
        var path = CreateCorruptFile();

        var success = _loader.TryGetInfo(path, out var info);

        success.Should().BeFalse();
        info.Should().BeNull();
    }

    #endregion
}
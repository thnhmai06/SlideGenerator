/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities.Tests
 * File: HardLinkTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Text;
using FluentAssertions;
using Xunit;

namespace SlideGenerator.Utilities.Tests;

/// <summary>
///     Unit tests for the <see cref="HardLink" /> utility class using temporary files on the same volume.
/// </summary>
public sealed class HardLinkTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    /// <summary>Cleans up all temporary files created during the test run.</summary>
    public void Dispose()
    {
        foreach (var f in _tempFiles.Where(File.Exists)) File.Delete(f);
    }

    private static string RandomString() => Guid.NewGuid().ToString("N");

    private string NewTempFile(string? content = null)
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        if (content is not null)
            File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    private string NewTempPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        _tempFiles.Add(path);
        return path;
    }

    #region Create

    /// <summary>
    ///     Verifies that <see cref="HardLink.Create" /> creates a link file that exists on disk
    ///     and whose content matches the original target.
    /// </summary>
    [Fact]
    public void Create_ValidPaths_LinkExistsWithSameContent()
    {
        var content = RandomString();
        var target = NewTempFile(content);
        var link = NewTempPath();

        HardLink.Create(link, target);

        File.Exists(link).Should().BeTrue();
        File.ReadAllText(link, Encoding.UTF8).Should().Be(content);
    }

    /// <summary>
    ///     Verifies that writing to the link path is reflected when reading via the target path,
    ///     confirming that a true hard link (shared inode) was created.
    /// </summary>
    [Fact]
    public void Create_ValidPaths_MutationViaLinkReflectedInTarget()
    {
        var original = RandomString();
        var modified = RandomString();
        var target = NewTempFile(original);
        var link = NewTempPath();

        HardLink.Create(link, target);
        File.WriteAllText(link, modified, Encoding.UTF8);

        File.ReadAllText(target, Encoding.UTF8).Should().Be(modified);
    }

    /// <summary>
    ///     Verifies that <see cref="HardLink.Create" /> with <c>force = true</c> (the default)
    ///     replaces an existing file at the link path without throwing.
    /// </summary>
    [Fact]
    public void Create_ForceTrue_ExistingFileAtLinkPathReplaced()
    {
        var targetContent = RandomString();
        var target = NewTempFile(targetContent);
        var link = NewTempFile(RandomString());

        var act = () => HardLink.Create(link, target, force: true);

        act.Should().NotThrow();
        File.ReadAllText(link, Encoding.UTF8).Should().Be(targetContent);
    }

    /// <summary>
    ///     Verifies that <see cref="HardLink.Create" /> with <c>force = false</c> throws
    ///     <see cref="IOException" /> when a file already exists at the link path.
    /// </summary>
    [Fact]
    public void Create_ForceFalse_ExistingFileAtLinkPath_ThrowsIOException()
    {
        var target = NewTempFile(RandomString());
        var link = NewTempFile(RandomString());

        var act = () => HardLink.Create(link, target, force: false);

        act.Should().Throw<IOException>();
    }

    /// <summary>
    ///     Verifies that <see cref="HardLink.Create" /> throws <see cref="IOException" />
    ///     when the target file does not exist.
    /// </summary>
    [Fact]
    public void Create_TargetDoesNotExist_ThrowsIOException()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        var link = NewTempPath();

        var act = () => HardLink.Create(link, nonExistent);

        act.Should().Throw<IOException>();
    }

    #endregion
}

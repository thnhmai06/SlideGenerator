/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities.Tests
 * File: FileAccessHelperTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using Xunit;

namespace SlideGenerator.Utilities.Tests;

/// <summary>
///     Unit tests for <see cref="FileAccessHelper" />.
/// </summary>
public sealed class FileAccessHelperTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var f in _tempFiles.Where(File.Exists)) File.Delete(f);
    }

    private string NewTempPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tmp");
        _tempFiles.Add(path);
        return path;
    }

    #region IsFileLockedException

    /// <summary>Verifies that a sharing-violation HResult (0x80070020) is recognized as a lock exception.</summary>
    [Fact]
    public void IsFileLockedException_SharingViolationHResult_ReturnsTrue()
    {
        var ex = new IOException("locked", unchecked((int)0x80070020));

        FileAccessHelper.IsFileLockedException(ex).Should().BeTrue();
    }

    /// <summary>Verifies that a lock-violation HResult (0x80070021) is recognized as a lock exception.</summary>
    [Fact]
    public void IsFileLockedException_LockViolationHResult_ReturnsTrue()
    {
        var ex = new IOException("locked", unchecked((int)0x80070021));

        FileAccessHelper.IsFileLockedException(ex).Should().BeTrue();
    }

    /// <summary>Verifies that an unrelated HResult is not recognized as a lock exception.</summary>
    [Fact]
    public void IsFileLockedException_UnrelatedHResult_ReturnsFalse()
    {
        var ex = new IOException("disk full", unchecked((int)0x80070027));

        FileAccessHelper.IsFileLockedException(ex).Should().BeFalse();
    }

    #endregion

    #region WaitForFileChangeAsync

    /// <summary>Verifies that the wait completes once the watched file is modified.</summary>
    [Fact]
    public async Task WaitForFileChangeAsync_FileModified_CompletesTask()
    {
        var ct = TestContext.Current.CancellationToken;
        var path = NewTempPath();
        await File.WriteAllTextAsync(path, "initial", ct);

        var waitTask = FileAccessHelper.WaitForFileChangeAsync(path, ct);
        await Task.Delay(100, ct);
        await File.WriteAllTextAsync(path, "changed", ct);

        var completed = await Task.WhenAny(waitTask, Task.Delay(5000, ct));
        completed.Should().Be(waitTask);
    }

    /// <summary>Verifies that the wait is cancellable via the supplied token.</summary>
    [Fact]
    public async Task WaitForFileChangeAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var path = NewTempPath();
        await File.WriteAllTextAsync(path, "initial", TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();

        var waitTask = FileAccessHelper.WaitForFileChangeAsync(path, cts.Token);
        cts.CancelAfter(100);

        var act = async () => await waitTask;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}

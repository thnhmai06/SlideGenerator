/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio.Tests
 * File: SingleInstanceLockTests.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Stdio.Implementations;
using Xunit;

namespace SlideGenerator.Stdio.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="SingleInstanceLock" />.
/// </summary>
public sealed class SingleInstanceLockTests : IDisposable
{
    private readonly string _mutexName = $"SingleInstanceLock.Test.{Guid.NewGuid()}";
    private readonly string _pidFilePath = Path.GetTempFileName();
    private readonly List<SingleInstanceLock> _locks = [];

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var l in _locks) l.Dispose();
        if (File.Exists(_pidFilePath)) File.Delete(_pidFilePath);
    }

    private SingleInstanceLock NewLock()
    {
        var l = new SingleInstanceLock(_mutexName, _pidFilePath);
        _locks.Add(l);
        return l;
    }

    #region Acquire

    /// <summary>The first call on a free mutex succeeds.</summary>
    [Fact]
    public void TryAcquire_FirstCall_ReturnsTrue()
    {
        NewLock().TryAcquire().Should().BeTrue();
    }

    /// <summary>The second call while the first holds the mutex returns false.</summary>
    [Fact]
    public void TryAcquire_SecondCallWhileFirstHeld_ReturnsFalse()
    {
        NewLock().TryAcquire();
        var second = NewLock();

        var thread = new Thread(() => second.TryAcquire().Should().BeFalse());
        thread.Start();
        thread.Join();
    }

    /// <summary>After the first lock is disposed of, a new instance can acquire the same mutex.</summary>
    [Fact]
    public void TryAcquire_AfterDispose_Succeeds()
    {
        var first = NewLock();
        first.TryAcquire();
        first.Dispose();
        _locks.Remove(first);

        NewLock().TryAcquire().Should().BeTrue();
    }

    #endregion

    #region PID content

    /// <summary>PID written to the file matches the current process ID.</summary>
    [Fact]
    public void TryAcquire_WritesCurrentProcessId()
    {
        var singleInstanceLock = NewLock();
        singleInstanceLock.TryAcquire();

        using var stream = new FileStream(_pidFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        reader.ReadToEnd().Should().Be(Environment.ProcessId.ToString());
    }

    /// <summary>ReadPid succeeds while the lock is held.</summary>
    [Fact]
    public void ReadPid_WhileLockHeld_ReturnsCorrectPid()
    {
        var singleInstanceLock = NewLock();
        singleInstanceLock.TryAcquire();

        singleInstanceLock.ReadPid().Should().Be(Environment.ProcessId);
    }

    /// <summary>ReadPid on a non-existent file returns null instead of throwing.</summary>
    [Fact]
    public void ReadPid_NonExistentFile_ReturnsNull()
    {
        var singleInstanceLock =
            new SingleInstanceLock(_mutexName, Path.Combine(Path.GetTempPath(), $"no-such-{Guid.NewGuid()}.pid"));

        singleInstanceLock.ReadPid().Should().BeNull();
    }

    #endregion

    #region PID file write protection

    /// <summary>PID file cannot be opened for writing while the lock is held.</summary>
    [Fact]
    public void FileStream_WriteWhileLocked_ThrowsIOException()
    {
        NewLock().TryAcquire();

        var act = () => new FileStream(_pidFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

        act.Should().Throw<IOException>();
    }

    /// <summary>
    ///     File.WriteAllText is blocked while the lock is held.
    ///     Windows only: Linux uses advisory locking, so File.WriteAllText does not throw.
    /// </summary>
    [Fact]
    public void WriteAllText_WhileLocked_ThrowsIOException()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("File write-lock exclusion via FileShare is mandatory on Windows only.");

        NewLock().TryAcquire();

        var act = () => File.WriteAllText(_pidFilePath, "overwrite");

        act.Should().Throw<IOException>();
    }

    /// <summary>
    ///     ReadPid succeeds on the owner instance while the lock is held.
    /// </summary>
    [Fact]
    public void ReadPid_WhileLocked_Succeeds()
    {
        var singleInstanceLock = NewLock();
        singleInstanceLock.TryAcquire();

        var act = singleInstanceLock.ReadPid;

        act.Should().NotThrow();
    }

    /// <summary>
    ///     A compatible reader can read while the lock owner keeps the PID file open for read/write.
    /// </summary>
    [Fact]
    public void FileStream_ReadWhileLockedWithCompatibleShare_ReturnsPid()
    {
        NewLock().TryAcquire();

        using var stream = new FileStream(_pidFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        reader.ReadToEnd().Should().Be(Environment.ProcessId.ToString());
    }

    /// <summary>
    ///     The PID file lock blocks <see cref="File.ReadAllText(string)"/> because its default share mode is incompatible
    ///     with the owner handle's write access on Windows. Use ReadPid instead.
    ///     Windows only: Linux uses advisory locking, so File.ReadAllText does not throw.
    /// </summary>
    [Fact]
    public void ReadAllText_WhileLocked_ThrowsIOException()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("File read-lock exclusion via FileShare is mandatory on Windows only.");

        NewLock().TryAcquire();

        var act = () => File.ReadAllText(_pidFilePath);

        act.Should().Throw<IOException>();
    }

    #endregion
}

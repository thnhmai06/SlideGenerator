/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: SingleInstanceLockTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Desktop.Bootstrap;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Behavioral tests for the desktop host's <see cref="SingleInstanceLock" /> guard: a second
///     instance must be rejected while the first holds the lock, and disposing must release the
///     mutex and remove the PID file.
/// </summary>
public sealed class SingleInstanceLockTests
{
    /// <summary>
    ///     A fresh lock with no competing instance must acquire successfully and report its PID.
    /// </summary>
    [Fact]
    public void TryAcquire_NoCompetingInstance_AcquiresAndReportsPid()
    {
        var pidFile = Path.Combine(Path.GetTempPath(), $"sg-{Guid.NewGuid():N}.pid");
        using var lock1 = new SingleInstanceLock($"sg-test-{Guid.NewGuid():N}", pidFile);

        var acquired = lock1.TryAcquire();

        acquired.Should().BeTrue();
        File.Exists(pidFile).Should().BeTrue();
        lock1.ReadPid().Should().Be(Environment.ProcessId);
    }

    /// <summary>
    ///     A second lock on the same mutex must fail while the first is still held. A mutex is owned per
    ///     thread, so the competing acquisition runs on a separate thread.
    /// </summary>
    [Fact]
    public void TryAcquire_SecondInstance_IsRejected()
    {
        var mutexName = $"sg-test-{Guid.NewGuid():N}";
        var pidFile1 = Path.Combine(Path.GetTempPath(), $"sg-{Guid.NewGuid():N}.pid");
        var pidFile2 = Path.Combine(Path.GetTempPath(), $"sg-{Guid.NewGuid():N}.pid");
        using var lock1 = new SingleInstanceLock(mutexName, pidFile1);
        using var lock2 = new SingleInstanceLock(mutexName, pidFile2);

        lock1.TryAcquire().Should().BeTrue();

        var acquired = false;
        var thread = new Thread(() => acquired = lock2.TryAcquire());
        thread.Start();
        thread.Join();

        acquired.Should().BeFalse();
        lock2.ReadPid().Should().BeNull();
    }

    /// <summary>
    ///     After <see cref="SingleInstanceLock.Dispose" /> the mutex must be free again and the PID file removed.
    /// </summary>
    [Fact]
    public void Dispose_ReleasesLockAndRemovesPidFile()
    {
        var mutexName = $"sg-test-{Guid.NewGuid():N}";
        var pidFile = Path.Combine(Path.GetTempPath(), $"sg-{Guid.NewGuid():N}.pid");
        var lock1 = new SingleInstanceLock(mutexName, pidFile);
        lock1.TryAcquire().Should().BeTrue();
        File.Exists(pidFile).Should().BeTrue();

        lock1.Dispose();

        File.Exists(pidFile).Should().BeFalse();
        using var lock2 = new SingleInstanceLock(mutexName, pidFile);
        lock2.TryAcquire().Should().BeTrue();
    }
}

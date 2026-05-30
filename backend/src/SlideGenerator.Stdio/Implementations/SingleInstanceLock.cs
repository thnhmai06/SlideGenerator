/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: SingleInstanceLock.cs
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

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     Cross-platform single-instance guard. A named <see cref="Mutex" /> is the authoritative lock;
///     a PID file (kept write-locked) carries the owning process ID so a competing instance can display it.
///     Registered in DI as a singleton after <see cref="TryAcquire" /> succeeds in <c>Program.cs</c>.
/// </summary>
/// <param name="mutexName">
///     OS mutex name — plain string, no <c>Global\</c> prefix (that prefix breaks on Unix).
/// </param>
/// <param name="pidFilePath">Path where the owning process ID is written.</param>
internal sealed class SingleInstanceLock(string mutexName, string pidFilePath) : IDisposable
{
    private Mutex? _mutex;
    private FileStream? _pidStream;

    /// <summary>
    ///     Tries to acquire the named mutex. On success, writes the current process ID to
    ///     the PID file and holds the file open so only this process can write while readers can use compatible sharing.
    /// </summary>
    /// <returns><see langword="true" /> if the lock was acquired; <see langword="false" /> if another instance holds it.</returns>
    public bool TryAcquire()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pidFilePath);

        _mutex = new Mutex(false, mutexName);
        try
        {
            if (!_mutex.WaitOne(0))
            {
                _mutex.Dispose();
                _mutex = null;
                return false;
            }
        }
        catch (AbandonedMutexException)
        {
            // Previous owner crashed without releasing — we now own it.
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(pidFilePath));
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        _pidStream = new FileStream(pidFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        using var writer = new StreamWriter(_pidStream, leaveOpen: true);
        writer.Write(Environment.ProcessId);
        writer.Flush();

        return true;
    }

    /// <summary>
    ///     Reads the PID from the PID file while the owner holds a write-exclusive lock.
    ///     Uses <see cref="FileShare.ReadWrite" /> so it is compatible with the owner's <see cref="FileAccess.ReadWrite" />.
    ///     Returns <see langword="null" /> if the file cannot be read.
    /// </summary>
    public int? ReadPid()
    {
        try
        {
            var stream = _pidStream ?? new FileStream(pidFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, leaveOpen: _pidStream is not null);
            var pid = int.Parse(reader.ReadToEnd());

            if (_pidStream is null) stream.Dispose();
            return pid;
        }
        catch { return null; }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pidStream?.Dispose();
        _pidStream = null;

        if (_mutex is null) return;
        try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
        _mutex.Dispose();
        _mutex = null;
    }
}

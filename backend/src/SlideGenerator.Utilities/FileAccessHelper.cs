/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Utilities
 * File: FileAccessHelper.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Utilities;

/// <summary>
///     Helpers for opening files that may be transiently locked by another process.
/// </summary>
public static class FileAccessHelper
{
    /// <summary>
    ///     Suspends the caller until a <see cref="FileSystemWatcher" /> event fires on
    ///     <paramref name="filePath" />, indicating the file may have been released.
    /// </summary>
    /// <param name="filePath">Absolute path of the file to watch.</param>
    /// <param name="ct">Token to cancel the wait.</param>
    public static async Task WaitForFileChangeAsync(string filePath, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        var file = Path.GetFileName(filePath);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watcher = new FileSystemWatcher(dir, file);
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Attributes | NotifyFilters.Size;
        watcher.EnableRaisingEvents = true;
        watcher.Changed += (_, _) => tcs.TrySetResult(true);
        watcher.Created += (_, _) => tcs.TrySetResult(true);
        watcher.Error += (_, e) => tcs.TrySetException(e.GetException());

        await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Returns <see langword="true" /> if <paramref name="ex" /> represents a Windows file-lock error.</summary>
    public static bool IsFileLockedException(IOException ex)
        // 0x80070020 = sharing violation, 0x80070021 = lock violation
        => ex.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021);
}

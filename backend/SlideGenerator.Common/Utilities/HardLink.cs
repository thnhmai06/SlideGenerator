/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generating
 * File: HardLink.cs
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

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SlideGenerator.Common.Utilities;

/// <summary>Cross-platform hard-link creation.</summary>
public static partial class HardLink
{
    /// <summary>
    ///     Creates a hard link at <paramref name="linkPath" /> pointing to the existing file at
    ///     <paramref name="targetPath" />. Both paths must reside on the same volume.
    /// </summary>
    /// <param name="linkPath">Path where the hard link will be created.</param>
    /// <param name="targetPath">Path to the existing file to link to.</param>
    /// <param name="force">
    ///     When <c>true</c> (default), deletes any existing file at <paramref name="linkPath" /> before creating
    ///     the link. When <c>false</c>, throws <see cref="IOException" /> if the path is already occupied.
    /// </param>
    /// <exception cref="IOException">The OS call failed or <paramref name="force" /> is false and the path exists.</exception>
    public static void Create(string linkPath, string targetPath, bool force = true)
    {
        if (force && File.Exists(linkPath)) File.Delete(linkPath);

        if (OperatingSystem.IsWindows())
            CreateWindows(linkPath, targetPath);
        else
            CreateUnix(linkPath, targetPath);
    }

    #region Windows

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLinkW(string lpFileName, string lpExistingFileName,
        IntPtr lpSecurityAttributes);

    private static void CreateWindows(string linkPath, string targetPath)
    {
        if (!CreateHardLinkW(linkPath, targetPath, IntPtr.Zero))
            throw new IOException(
                $"CreateHardLinkW failed (Win32 error {Marshal.GetLastWin32Error()}): {linkPath} → {targetPath}");
    }

    #endregion

    #region Unix

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    // ReSharper disable once InconsistentNaming
    private static partial int link(string oldpath, string newpath);

    private static void CreateUnix(string linkPath, string targetPath)
    {
        if (link(targetPath, linkPath) != 0)
            throw new IOException(
                new Win32Exception(Marshal.GetLastWin32Error()).Message +
                $" link({targetPath}, {linkPath})");
    }

    #endregion
}
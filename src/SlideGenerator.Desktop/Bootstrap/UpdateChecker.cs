/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: UpdateChecker.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Serilog;
using Velopack;
using Velopack.Sources;

namespace SlideGenerator.Desktop.Bootstrap;

/// <summary>The outcome of <see cref="UpdateChecker.CheckForUpdatesAsync" /> — lets a caller with a UI (the
///     Settings page's "Kiểm tra cập nhật" button) show what happened, while the startup fire-and-forget call
///     keeps ignoring the result.</summary>
internal enum UpdateCheckResult
{
    /// <summary>Not running as an installed Velopack app (e.g. `dotnet run`, portable) — nothing to check.</summary>
    NotInstalled,

    /// <summary>Already on the latest version.</summary>
    UpToDate,

    /// <summary>A newer version was downloaded; applies on next restart.</summary>
    UpdateDownloaded,

    /// <summary>The check or download failed (network error, GitHub unreachable, etc.).</summary>
    Failed
}

/// <summary>
///     Checks GitHub Releases for a newer version via Velopack.
/// </summary>
internal static class UpdateChecker
{
    /// <summary>Checks for updates, logs the outcome, and returns it for a caller that wants to show it.</summary>
    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        var manager = new UpdateManager(new GithubSource(Metadata.Value.Repository, null, false));
        if (!manager.IsInstalled)
        {
            Log.Debug("Skipping update check: not running as an installed Velopack app.");
            return UpdateCheckResult.NotInstalled;
        }

        try
        {
            var newVersion = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (newVersion is null)
            {
                Log.Information("No update available. Current version: {Version}", manager.CurrentVersion);
                return UpdateCheckResult.UpToDate;
            }

            Log.Information("Update available: {Version}. Downloading...", newVersion.TargetFullRelease.Version);
            await manager.DownloadUpdatesAsync(newVersion).ConfigureAwait(false);
            Log.Information("Update downloaded. Will apply on next restart.");
            return UpdateCheckResult.UpdateDownloaded;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed.");
            return UpdateCheckResult.Failed;
        }
    }
}
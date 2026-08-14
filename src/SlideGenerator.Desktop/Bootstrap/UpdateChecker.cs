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
using SlideGenerator.Settings.Immutable;
using Velopack;
using Velopack.Sources;

namespace SlideGenerator.Desktop.Bootstrap;

/// <summary>
///     Checks GitHub Releases for a newer version via Velopack. Only logs — do not yet surface a
///     dialog/notification, since there is no ViewModel/View to show one from yet.
/// </summary>
/// <remarks>TODO: hook into a real notification once the main UI exists.</remarks>
internal static class UpdateChecker
{
    /// <summary>Checks for updates and logs the outcome. No-op (logs and returns) when not running as an installed app.</summary>
    public static async Task CheckForUpdatesAsync()
    {
        var manager = new UpdateManager(new GithubSource(NameAndPaths.Application.Repository, null, false));
        if (!manager.IsInstalled)
        {
            Log.Debug("Skipping update check: not running as an installed Velopack app.");
            return;
        }

        try
        {
            var newVersion = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (newVersion is null)
            {
                Log.Information("No update available. Current version: {Version}", manager.CurrentVersion);
                return;
            }

            Log.Information("Update available: {Version}. Downloading...", newVersion.TargetFullRelease.Version);
            await manager.DownloadUpdatesAsync(newVersion).ConfigureAwait(false);
            Log.Information("Update downloaded. Will apply on next restart.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed.");
        }
    }
}
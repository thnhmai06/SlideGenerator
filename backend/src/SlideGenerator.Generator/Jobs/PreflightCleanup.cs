/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: PreflightCleanup.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     Runs first when a fresh (non-resumed) job starts, to overwrite any prior output file left by an
///     earlier run of this exact job. Only ever touches this job's own output path — never its parent
///     directory or sibling files, since other jobs may share the same output folder.
/// </summary>
internal static class PreflightCleanup
{
    /// <summary>Deletes <paramref name="outputPath" /> if it exists and ensures its parent directory exists.</summary>
    internal static void Run(string outputPath, ILogger logger)
    {
        try
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(outputPath)) return;
            
            File.Delete(outputPath);
            logger.LogInformation("Removed prior output file '{OutputPath}'", outputPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up prior output at '{OutputPath}'", outputPath);
        }
    }
}

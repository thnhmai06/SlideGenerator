/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobOutput.cs
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
///     The working (<c>.partial</c>) file a job writes to before committing: the presentation is only moved
///     onto the job's final output path once every phase has succeeded, so an existing result survives a failed
///     or cancelled run and a half-written file is never mistaken for one.
/// </summary>
internal static class JobOutput
{
    /// <summary>
    ///     Returns the working file for <paramref name="outputPath" />: the same file name with
    ///     <c>.partial</c> appended to the stem, in the same directory (e.g. <c>C:\o\a.pptx</c> becomes
    ///     <c>C:\o\a.partial.pptx</c>). The presentation extension is kept because the presentation opener
    ///     derives the file type from it.
    /// </summary>
    /// <param name="outputPath">The job's final output path (absolute or relative).</param>
    /// <returns>The absolute working path next to the final output.</returns>
    public static string GetPartialPath(string outputPath)
    {
        outputPath = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(outputPath);
        var name = Path.GetFileNameWithoutExtension(outputPath);
        var extension = Path.GetExtension(outputPath);
        var partialName = $"{name}.partial{extension}";
        return string.IsNullOrEmpty(dir) ? partialName : Path.Combine(dir, partialName);
    }

    /// <summary>
    ///     Deletes the working file of <paramref name="outputPath" /> if it exists, leaving the final output
    ///     itself untouched. Used when a job ends without completing; an I/O failure is logged, never thrown.
    /// </summary>
    /// <param name="outputPath">The job's final output path.</param>
    /// <param name="logger">Logger for the cleanup outcome.</param>
    public static void DiscardPartial(string outputPath, ILogger logger)
    {
        var partialPath = GetPartialPath(outputPath);
        try
        {
            if (!File.Exists(partialPath)) return;

            File.Delete(partialPath);
            logger.LogInformation("Removed working file '{PartialPath}'", partialPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Failed to clean up working file at '{PartialPath}'", partialPath);
        }
    }
}

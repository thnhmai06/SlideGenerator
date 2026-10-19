/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: JobTempFolder.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Generator.Jobs;

/// <summary>
///     Per-job download cache folder: <c>%TEMP%\SlideGenerator\{workflowId}\{jobId}\</c>. Kept isolated per
///     job so concurrent jobs never contend on the same file. Shared between
///     <c>JobExecutor</c> (writes into it during Phase D) and
///     <c>SlideGenerationWorkflow</c> (deletes it once the job reaches a terminal outcome).
/// </summary>
internal static class JobTempFolder
{
    internal static string GetPath(string workflowId, int jobId)
    {
        return Path.Combine(NameAndPaths.TempFolder.RootPath, workflowId, jobId.ToString());
    }
}
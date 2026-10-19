/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowTempFolder.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Generator.Workflows;

/// <summary>
///     Per-workflow download cache folder: <c>%TEMP%\SlideGenerator\{requestId}\{workflowId}\</c>. Kept isolated per
///     workflow so concurrent workflows never contend on the same file. Shared between
///     <c>Runner.SlideGenerationRunner</c> (writes into it during Phase D) and
///     <c>GeneratorWorkflowObserver</c> (deletes it once the workflow reaches a terminal, non-Paused outcome).
/// </summary>
internal static class WorkflowTempFolder
{
    internal static string GetPath(string requestId, int workflowId)
    {
        return Path.Combine(NameAndPaths.TempFolder.RootPath, requestId, workflowId.ToString());
    }
}
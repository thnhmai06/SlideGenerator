/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: CreateTemplateIntegrationTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Xunit;

namespace SlideGenerator.Generator.Tests.Integration;

/// <summary>
///     Integration tests for <see cref="SlideGenerator.Generator.Application.Steps.CreateTemplate" />
///     verifying multi-worksheet output preservation (BUG-7) and PreflightCleanup behavior (BUG-7).
/// </summary>
public sealed class CreateTemplateIntegrationTests
{
    /// <summary>
    ///     INTEGRATION (BUG-7): Two sheets sharing the same workbook must yield two surviving
    ///     output presentations after the full workflow completes. Verifies that
    ///     <c>PreflightCleanup</c> wipes the output once before Phase A, and <c>CreateTemplate</c>
    ///     does not delete sibling sheets' output with <c>Directory.Delete</c>.
    ///     TODO: provide fixture under <c>tests/fixtures/bug-7/twosheets.xlsx</c> and
    ///     <c>tests/fixtures/bug-7/template.pptx</c>.
    /// </summary>
    [Fact(
        DisplayName = "INTEGRATION (BUG-7): two-worksheet workbook preserves both outputs — TODO fixture",
        Skip = "TODO: provide fixture under tests/fixtures/bug-7/")]
    public Task CreateTemplate_TwoSheetsSameWorkbook_BothOutputsPreserved()
    {
        Assert.Skip(
            "TODO: provide real .xlsx + .pptx fixture at tests/fixtures/bug-7/; see plan h-ng-x-l-keen-pelican.md");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     INTEGRATION (BUG-7): <c>PreflightCleanup</c> must delete only the target output
    ///     directories derived from the recipe nodes, not sibling or unrelated directories.
    ///     TODO: provide fixture under <c>tests/fixtures/bug-7/</c>.
    /// </summary>
    [Fact(
        DisplayName = "INTEGRATION (BUG-7): PreflightCleanup wipes only target output dirs — TODO fixture",
        Skip = "TODO: provide fixture under tests/fixtures/bug-7/")]
    public Task PreflightCleanup_RunBeforePhaseA_WipesOnlyTargetOutputDirs()
    {
        Assert.Skip(
            "TODO: provide real .xlsx + .pptx fixture at tests/fixtures/bug-7/; see plan h-ng-x-l-keen-pelican.md");
        return Task.CompletedTask;
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: PreflightCleanupTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Application.Steps;
using SlideGenerator.Generator.Tests.Unit.Helpers;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="PreflightCleanup" />, locking its per-job overwrite-only behavior
///     (no longer deletes the whole parent output directory — see <c>JobSpec.OutputPath</c> only).
/// </summary>
public sealed class PreflightCleanupTests
{
    /// <summary>Verifies an existing output file for this job is deleted.</summary>
    [Fact]
    public void Run_OutputFileExists_DeletesIt()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        var outputPath = data.Persist.Specification.OutputPath;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, "stale");

        try
        {
            new PreflightCleanup().Run(ctx);

            File.Exists(outputPath).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    /// <summary>Verifies no exception is thrown when the output file does not exist yet.</summary>
    [Fact]
    public void Run_OutputFileMissing_DoesNotThrow()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        var outputPath = data.Persist.Specification.OutputPath;
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var act = () => new PreflightCleanup().Run(ctx);

        act.Should().NotThrow();
    }

    /// <summary>Verifies a sibling file in the same output directory (belonging to another job) survives.</summary>
    [Fact]
    public void Run_SiblingFileInSameDirectory_IsNotDeleted()
    {
        var (ctx, data) = StepHelper.CreateContextPair();
        var dir = Path.GetDirectoryName(data.Persist.Specification.OutputPath)!;
        Directory.CreateDirectory(dir);
        var siblingPath = Path.Combine(dir, "sibling-job-output.pptx");
        File.WriteAllText(siblingPath, "belongs to another job");

        try
        {
            new PreflightCleanup().Run(ctx);

            File.Exists(siblingPath).Should().BeTrue();
        }
        finally
        {
            File.Delete(siblingPath);
        }
    }
}

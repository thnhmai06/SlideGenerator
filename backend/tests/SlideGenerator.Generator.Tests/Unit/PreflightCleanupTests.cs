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
using Microsoft.Extensions.Logging;
using NSubstitute;
using SlideGenerator.Generator.Services;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="PreflightCleanup" />, locking its per-job overwrite-only behavior
///     (never touches the parent output directory or sibling files).
/// </summary>
public sealed class PreflightCleanupTests
{
    private static readonly ILogger Logger = Substitute.For<ILogger>();

    /// <summary>Verifies an existing output file for this job is deleted.</summary>
    [Fact]
    public void Run_OutputFileExists_DeletesIt()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(dir, "output.pptx");
        Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, "stale");

        try
        {
            PreflightCleanup.Run(outputPath, Logger);

            File.Exists(outputPath).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    /// <summary>Verifies no exception is thrown when the output file does not exist yet.</summary>
    [Fact]
    public void Run_OutputFileMissing_DoesNotThrow()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "output.pptx");

        var act = () => PreflightCleanup.Run(outputPath, Logger);

        act.Should().NotThrow();
    }

    /// <summary>Verifies a sibling file in the same output directory (belonging to another job) survives.</summary>
    [Fact]
    public void Run_SiblingFileInSameDirectory_IsNotDeleted()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var outputPath = Path.Combine(dir, "output.pptx");
        var siblingPath = Path.Combine(dir, "sibling-job-output.pptx");
        File.WriteAllText(siblingPath, "belongs to another job");

        try
        {
            PreflightCleanup.Run(outputPath, Logger);

            File.Exists(siblingPath).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

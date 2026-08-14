/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: ServiceTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Document.Presentations.Identifiers;
using SlideGenerator.Document.Workbooks.Identifiers;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Recipe.Models;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>
///     Unit tests for <see cref="Service" />'s pure helper methods: <c>BuildJobs</c> (flattens a recipe's
///     <c>Mappings</c> into one <see cref="JobSpecification" /> per worksheet source) and
///     <c>DeriveStatus</c> (multi-job status aggregation for one request's job list).
/// </summary>
public sealed class ServiceTests
{
    #region BuildJobs

    private static Recipe.Models.Recipe OneMappingOneSource()
    {
        var source = new WorksheetSource(
            new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "data.xlsx")),
            new WorksheetIdentifier("Sheet1"));
        var mapping = new Mapping(
            [source],
            new PresentationSource(
                new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx")),
                new SlideIdentifier(1)),
            [], []);
        return new Recipe.Models.Recipe([mapping]);
    }

    /// <summary>Verifies that one mapping with one source produces exactly one job with resolved values.</summary>
    [Fact]
    public void BuildJobs_OneMappingOneSource_ProducesOneJob()
    {
        var recipe = OneMappingOneSource();
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().HaveCount(1);
        var job = jobs[0];
        job.WorksheetName.Should().Be("Sheet1");
        job.TemplateSlideIndex.Should().Be(1);
        job.OutputPath.Should().Contain("Sheet1");
    }

    /// <summary>Verifies that a mapping with N sources produces N jobs, all sharing the mapping's template/instructions.</summary>
    [Fact]
    public void BuildJobs_MappingWithMultipleSources_ProducesOneJobPerSource()
    {
        var mapping = new Mapping(
            [
                new WorksheetSource(new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "a.xlsx")),
                    new WorksheetIdentifier("A")),
                new WorksheetSource(new WorkbookIdentifier(Path.Combine(Path.GetTempPath(), "b.xlsx")),
                    new WorksheetIdentifier("B"))
            ],
            new PresentationSource(
                new PresentationIdentifier(Path.Combine(Path.GetTempPath(), "template.pptx")),
                new SlideIdentifier(1)),
            [], []);
        var recipe = new Recipe.Models.Recipe([mapping]);
        var request = new Request(1, "Test", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().HaveCount(2);
        jobs.Select(j => j.WorksheetName).Should().Contain(["A", "B"]);
    }

    /// <summary>Verifies that an empty recipe (no mappings) produces no jobs.</summary>
    [Fact]
    public void BuildJobs_EmptyRecipe_ProducesNoJobs()
    {
        var recipe = new Recipe.Models.Recipe([]);
        var request = new Request(1, "Empty", PresentationType.Pptx, Path.GetTempPath());

        var jobs = Service.BuildJobs(recipe, request);

        jobs.Should().BeEmpty();
    }

    #endregion

    #region DeriveStatus

    private static JobSnapshot Job(JobStatus jobStatus)
    {
        return new JobSnapshot(
            "req", 0, jobStatus, JobPhase.CreatingOutput, 0,
            new JobSpecification("wb", "Sheet1", null, null, "ppt", 1, [], [], "out.pptx"),
            DateTimeOffset.UtcNow);
    }

    /// <summary>Any job Pending or Running → the whole request is Running.</summary>
    [Fact]
    public void DeriveStatus_AnyPendingOrRunning_ReturnsRunning()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Pending), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Running);
    }

    /// <summary>No job Pending/Running but at least one Paused → Paused.</summary>
    [Fact]
    public void DeriveStatus_NoneRunningSomePaused_ReturnsPaused()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Paused) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Paused);
    }

    /// <summary>Every job Canceled → Canceled.</summary>
    [Fact]
    public void DeriveStatus_AllCancelled_ReturnsCancelled()
    {
        var jobs = new[] { Job(JobStatus.Cancelled), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Cancelled);
    }

    /// <summary>Every job Complete → Complete.</summary>
    [Fact]
    public void DeriveStatus_AllComplete_ReturnsComplete()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Complete) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Complete);
    }

    /// <summary>A mix of Complete and Canceled (no Pending/Running/Paused) falls back to Complete.</summary>
    [Fact]
    public void DeriveStatus_MixedCompleteAndCancelled_ReturnsComplete()
    {
        var jobs = new[] { Job(JobStatus.Complete), Job(JobStatus.Cancelled) };

        Service.DeriveStatus(jobs).Should().Be(JobStatus.Complete);
    }

    #endregion
}
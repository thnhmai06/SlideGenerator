/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator.Tests
 * File: RequestStatusRulesTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using SlideGenerator.Generator.Jobs.Models;
using Xunit;

namespace SlideGenerator.Generator.Tests.Unit;

/// <summary>Unit tests for <see cref="RequestStatusRules" /> — the single request-level status derivation shared by the service and the UI.</summary>
public sealed class RequestStatusRulesTests
{
    /// <summary>Verifies every aggregate status combination, including a workflow-level pause flag.</summary>
    [Theory]
    [MemberData(nameof(DeriveCases))]
    public void Derive_JobStatuses_ReturnsExpectedStatus(JobStatus[] jobs, bool workflowPaused, RequestStatus expected)
    {
        RequestStatusRules.Derive(jobs, workflowPaused).Should().Be(expected);
    }

    /// <summary>Cases covering empty, running, paused, cancelled, errored, mixed, and workflow-paused inputs.</summary>
    public static TheoryData<JobStatus[], bool, RequestStatus> DeriveCases => new()
    {
        { [], false, RequestStatus.Complete },
        { [JobStatus.Pending], false, RequestStatus.Running },
        { [JobStatus.Running], false, RequestStatus.Running },
        { [JobStatus.Running, JobStatus.Error], false, RequestStatus.Running },
        { [JobStatus.Paused, JobStatus.Complete], false, RequestStatus.Paused },
        { [JobStatus.Cancelled, JobStatus.Cancelled], false, RequestStatus.Cancelled },
        { [JobStatus.Error, JobStatus.Error], false, RequestStatus.Error },
        { [JobStatus.Complete, JobStatus.Error], false, RequestStatus.CompletedWithError },
        { [JobStatus.Cancelled, JobStatus.Error], false, RequestStatus.CompletedWithError },
        { [JobStatus.Complete, JobStatus.Cancelled], false, RequestStatus.Complete },
        { [JobStatus.Complete, JobStatus.Complete], false, RequestStatus.Complete },
        { [JobStatus.Running], true, RequestStatus.Paused },
        { [JobStatus.Pending, JobStatus.Complete], true, RequestStatus.Paused },
        { [JobStatus.Complete], true, RequestStatus.Complete },
        { [JobStatus.Error, JobStatus.Error], true, RequestStatus.Error }
    };
}

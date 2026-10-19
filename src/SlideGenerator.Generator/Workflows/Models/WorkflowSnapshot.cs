/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowSnapshot.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;

namespace SlideGenerator.Generator.Workflows.Models;

/// <summary>
///     The state of one workflow — the execution of one <see cref="Request" />, which fans out into one job per
///     worksheet/presentation pair. Only the workflow's own lifecycle lives here (it is what pause/resume/stop act
///     on); per-job progress is carried by <see cref="JobSnapshot" />.
/// </summary>
/// <param name="WorkflowId">The workflow id (also the id that groups the workflow's jobs).</param>
/// <param name="Status">
///     Workflow lifecycle: <see cref="JobStatus.Pending" />, <see cref="JobStatus.Running" />,
///     <see cref="JobStatus.Paused" />, <see cref="JobStatus.Complete" />, <see cref="JobStatus.Cancelled" /> or
///     <see cref="JobStatus.Error" />.
/// </param>
/// <param name="Timestamp">When this state was produced.</param>
public sealed record WorkflowSnapshot(string WorkflowId, JobStatus Status, DateTimeOffset Timestamp);

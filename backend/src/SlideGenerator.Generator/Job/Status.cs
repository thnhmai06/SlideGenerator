/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: Status.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Job;

/// <summary>Identifies the execution status of a job.</summary>
public enum Status
{
    /// <summary>Spawned but not yet picked up for execution by <c>JobRunner</c>.</summary>
    Pending,
    Running,
    Complete,
    Paused,
    Cancelled,
    Error
}
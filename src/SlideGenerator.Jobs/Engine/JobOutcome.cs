/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: JobOutcome.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>A job's terminal outcome, as decided solely by <see cref="IJobEngine{TKey,TState}" />.</summary>
public enum JobOutcome : byte
{
    /// <summary><see cref="IJobWorkload{TState}.RunAsync" /> returned normally.</summary>
    Completed,

    /// <summary>The job was stopped (or the engine was shut down) before it completed.</summary>
    Cancelled,

    /// <summary><see cref="IJobWorkload{TState}.RunAsync" /> threw an exception other than cancellation.</summary>
    Faulted
}
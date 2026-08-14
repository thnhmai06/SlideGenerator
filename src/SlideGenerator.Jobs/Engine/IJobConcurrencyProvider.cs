/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Jobs
 * File: IJobConcurrencyProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Jobs.Engine;

/// <summary>
///     Supplies the current job concurrency limit. Re-read by the engine on every job start, so a change
///     applies to newly started jobs without a restart — jobs already waiting are unaffected (see
///     <see cref="IJobEngine{TKey,TState}" /> remarks on semaphore swap-not-resize).
/// </summary>
public interface IJobConcurrencyProvider
{
    int MaxConcurrentJobs { get; }
}
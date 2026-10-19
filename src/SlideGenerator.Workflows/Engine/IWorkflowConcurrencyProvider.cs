/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Workflows
 * File: IWorkflowConcurrencyProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Workflows.Engine;

/// <summary>
///     Supplies the current workflow concurrency limit. Re-read by the engine on every workflow start, so a change
///     applies to newly started workflows without a restart — workflows already waiting are unaffected (see
///     <see cref="IWorkflowEngine{TKey,TState}" /> remarks on semaphore swap-not-resize).
/// </summary>
public interface IWorkflowConcurrencyProvider
{
    int MaxConcurrentWorkflows { get; }
}
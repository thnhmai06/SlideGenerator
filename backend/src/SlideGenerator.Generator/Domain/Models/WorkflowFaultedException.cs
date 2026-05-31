/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowFaultedException.cs
 *
 * This file is part of this solution. 
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Generator.Domain.Models;

/// <summary>
///     Sentinel exception used when a primary step is about to propagate an unrecoverable
///     fault (e.g. <see cref="NullReferenceException" /> arising from a botched persistence
///     resume) and must notify Coordinator secondaries before the workflow host catches it.
///     Distinguishes "domain failure inside the step" from "infrastructure fault that
///     WorkflowCore must handle".
/// </summary>
public sealed class WorkflowFaultedException : Exception
{
    /// <inheritdoc />
    public WorkflowFaultedException(string message, Exception? inner) : base(message, inner)
    {
    }
}
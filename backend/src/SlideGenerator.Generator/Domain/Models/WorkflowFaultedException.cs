/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: WorkflowFaultedException.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
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
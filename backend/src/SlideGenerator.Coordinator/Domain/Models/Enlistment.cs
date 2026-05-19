/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: Enlistment.cs
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

namespace SlideGenerator.Coordinator.Domain.Models;

/// <summary>
///     Represents a participation model in the workflow coordination process.
///     Determines the role (primary or secondary) of a participant in handling
///     operations for a specific workflow key.
/// </summary>
public abstract record Enlistment;

/// <summary>
///     Returned to the first caller: this step owns the operation.
///     Call <see cref="SubmitResult" /> with the output path on success, or
///     <see cref="SubmitException" /> with the failure cause. Exactly one of the two must be
///     invoked — otherwise secondary waiters hang forever.
/// </summary>
/// <param name="SubmitResult">
///     Reports the produced output path (or <c>null</c> if the primary completed but produced no
///     artifact).
/// </param>
/// <param name="SubmitException">
///     Faults every secondary's <see cref="SecondaryEnlistment.WaitTask" /> with the supplied
///     exception.
/// </param>
public sealed record PrimaryEnlistment(
    Action<string?> SubmitResult,
    Action<Exception> SubmitException) : Enlistment;

/// <summary>
///     Returned to every subsequent caller: await <see cref="WaitTask" /> to get
///     the primary's output path (<c>null</c> if the primary failed).
/// </summary>
public sealed record SecondaryEnlistment(Task<string?> WaitTask) : Enlistment;
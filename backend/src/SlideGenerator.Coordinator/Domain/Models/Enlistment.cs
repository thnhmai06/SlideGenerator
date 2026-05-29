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
///     Represents the role assigned to a caller that enlists a key.
///     The first caller becomes the owner; all following callers become waiters.
/// </summary>
public abstract record Enlistment;

/// <summary>
///     Assigned to the first caller for a given key: this caller owns the operation and is
///     responsible for producing (or failing) the result.
///     Exactly one of <see cref="SubmitResult" /> or <see cref="SubmitException" /> must be
///     invoked — otherwise all waiters hang indefinitely.
/// </summary>
/// <param name="SubmitResult">
///     Completes the shared result with the supplied value (<c>null</c> is a valid outcome
///     meaning the operation produced no artifact).
/// </param>
/// <param name="SubmitException">
///     Faults every waiter's <see cref="WaiterEnlistment.WaitTask" /> with the supplied exception.
/// </param>
public sealed record OwnerEnlistment(
    Action<string?> SubmitResult,
    Action<Exception> SubmitException) : Enlistment;

/// <summary>
///     Assigned to every caller after the first for the same key.
///     Await <see cref="WaitTask" /> to receive the owner's result
///     (<c>null</c> if the owner completed with no artifact, or faulted if the owner called
///     <see cref="OwnerEnlistment.SubmitException" />).
/// </summary>
public sealed record WaiterEnlistment(Task<string?> WaitTask) : Enlistment;
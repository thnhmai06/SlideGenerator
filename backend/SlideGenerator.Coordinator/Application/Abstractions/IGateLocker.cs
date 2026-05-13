/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: IGateLocker.cs
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

using SlideGenerator.Coordinator.Domain.Models;

namespace SlideGenerator.Coordinator.Application.Abstractions;

/// <summary>
///     Defines a concurrency gate that limits parallel operations per <see cref="GateType" />.
///     Consumers depend on this abstraction; the concrete scheduling mechanism is in Infrastructure.
/// </summary>
public interface IGateLocker : IDisposable
{
    /// <summary>
    ///     Asynchronously waits to acquire a slot for the specified gate.
    /// </summary>
    ValueTask AcquireAsync(GateType gate, CancellationToken ct = default);

    /// <summary>
    ///     Tries to acquire a slot immediately without blocking.
    /// </summary>
    bool TryAcquire(GateType gate);

    /// <summary>
    ///     Releases a previously acquired slot for the specified gate.
    /// </summary>
    void Release(GateType gate);
}
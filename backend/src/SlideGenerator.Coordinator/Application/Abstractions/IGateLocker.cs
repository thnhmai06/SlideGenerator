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

namespace SlideGenerator.Coordinator.Application.Abstractions;

/// <summary>
///     Limits the number of concurrent operations per gate value of <typeparamref name="TGate" />.
///     Consumers depend on this abstraction; the concrete scheduling mechanism is in Infrastructure.
/// </summary>
/// <typeparam name="TGate">An enum whose values each represent an independent concurrency gate.</typeparam>
public interface IGateLocker<in TGate> : IDisposable where TGate : struct, Enum
{
    /// <summary>
    ///     Asynchronously waits until a slot is available for the specified gate, then acquires it.
    /// </summary>
    /// <param name="gate">The gate to acquire.</param>
    /// <param name="ct">Token that cancels the wait.</param>
    ValueTask AcquireAsync(TGate gate, CancellationToken ct = default);

    /// <summary>
    ///     Attempts to acquire a slot for the specified gate immediately without blocking.
    /// </summary>
    /// <param name="gate">The gate to acquire.</param>
    /// <returns><see langword="true" /> if the slot was acquired; <see langword="false" /> if the gate is at capacity.</returns>
    bool TryAcquire(TGate gate);

    /// <summary>
    ///     Releases a previously acquired slot for the specified gate and admits the next waiter if any.
    /// </summary>
    /// <param name="gate">The gate to release.</param>
    void Release(TGate gate);
}
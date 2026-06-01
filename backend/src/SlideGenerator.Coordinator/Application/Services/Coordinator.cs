/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: Coordinator.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;
using SlideGenerator.Coordinator.Application.Abstractions;
using SlideGenerator.Coordinator.Domain.Models;

namespace SlideGenerator.Coordinator.Application.Services;

/// <inheritdoc />
internal sealed class Coordinator : ICoordinator
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string?>> _entries
        = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Enlistment Enlist(string key)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (_entries.TryAdd(key, tcs))
            return new OwnerEnlistment(
                outputPath => tcs.TrySetResult(outputPath),
                ex => tcs.TrySetException(ex));

        return new WaiterEnlistment(_entries[key].Task);
    }
}
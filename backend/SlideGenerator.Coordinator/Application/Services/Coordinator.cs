/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Coordinator
 * File: Coordinator.cs
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
            return new PrimaryEnlistment(outputPath => tcs.TrySetResult(outputPath));

        return new SecondaryEnlistment(_entries[key].Task);
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: LogNotifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Application.Abstractions;
using SlideGenerator.Generator.Domain.Models.Data;

namespace SlideGenerator.Stdio.Implementations;

/// <summary>
///     A lightweight, in-process event bus for scoped log line notifications. Decouples
///     <c>Middleware</c> (Generator, publisher) from <see cref="ProgressCoalescer" /> (subscriber),
///     mirroring <see cref="GeneratingEventBus" />'s pattern for <c>IEventBus</c>.
/// </summary>
internal sealed class LogNotifier : ILogNotifier
{
    /// <summary>Publishes a <see cref="LogEntry" /> to all current subscribers. Safe to call from any thread.</summary>
    public void Publish(LogEntry entry) => OnLogEntry?.Invoke(entry);

    /// <summary>Raised whenever a log line is written through a workflow-scoped file logger.</summary>
    public event Action<LogEntry>? OnLogEntry;
}

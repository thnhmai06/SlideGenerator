/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Generator
 * File: ILogNotifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Domain.Models.Data;

namespace SlideGenerator.Generator.Application.Abstractions
{
    /// <summary>
    ///     Defines a publish-only sink for real-time log line notifications, scoped to Request/Job/Row.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This interface is owned by the <c>Pipeline</c> module (the publisher — <c>Middleware</c>
    ///         wires it into <c>IFileLoggerFactory.CreateFile</c>'s callback). The concrete implementation
    ///         lives in <c>SlideGenerator.Stdio</c> and forwards entries as <c>log/entries</c> JSON-RPC
    ///         notifications, mirroring <see cref="IEventBus" />'s <c>dep-interface-ownership</c> pattern.
    ///     </para>
    /// </remarks>
    public interface ILogNotifier
    {
        /// <summary>Publishes a single scoped log line. Safe to call from any thread.</summary>
        void Publish(LogEntry entry);
    }
}

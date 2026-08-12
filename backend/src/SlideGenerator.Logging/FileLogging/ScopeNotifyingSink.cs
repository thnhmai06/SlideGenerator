/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: ScopeNotifyingSink.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using Serilog.Core;
using Serilog.Events;

using SlideGenerator.Logging;
namespace SlideGenerator.Logging.FileLogging;

/// <summary>
///     Serilog sink that forwards every log event to a callback as a <see cref="LogNotification" />,
///     carrying the scope path built from whichever of <paramref name="scopePropertyNames" /> are present.
///     Runs alongside the file sink — never the only sink, since it does not itself persist anything.
/// </summary>
/// <param name="scopePropertyNames">Ordered property names to join into <see cref="LogNotification.Location" />.</param>
/// <param name="onLogEvent">Callback invoked for every emitted event.</param>
internal sealed class ScopeNotifyingSink(IReadOnlyList<string> scopePropertyNames, Action<LogNotification> onLogEvent)
    : ILogEventSink
{
    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        onLogEvent(new LogNotification
        {
            Timestamp = logEvent.Timestamp,
            Location = logEvent.BuildScopePath(scopePropertyNames),
            Level = logEvent.Level,
            Message = logEvent.RenderMessage(CultureInfo.InvariantCulture)
        });
    }
}

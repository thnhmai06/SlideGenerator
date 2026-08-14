/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Logging
 * File: Utilities.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Serilog.Events;
using Serilog.Formatting;

namespace SlideGenerator.Logging;

/// <summary>
///     Shared helpers for <see cref="ITextFormatter" /> implementations.
/// </summary>
internal static class Utilities
{
    /// <param name="logEvent">The event containing structured properties.</param>
    extension(LogEvent logEvent)
    {
        /// <summary>
        ///     Reads a scalar Serilog property value as text.
        /// </summary>
        /// <param name="propertyName">The name of the scalar property to read.</param>
        /// <returns>The scalar value as text, or <see langword="null" /> when it is missing or not scalar.</returns>
        public string? GetScalarValue(string propertyName)
        {
            return logEvent.Properties.TryGetValue(propertyName, out var value) &&
                   value is ScalarValue { Value: not null } scalar
                ? scalar.Value.ToString()
                : null;
        }

        /// <summary>
        ///     Joins whichever of <paramref name="propertyNames" /> are present as scalar properties on
        ///     <paramref name="logEvent" /> (pushed via <c>Serilog.Context.LogContext.PushProperty</c>) into a
        ///     single <c>/</c>-separated path, in the given order — segments whose property is absent are
        ///     skipped. This module has no notion of what the scope means; callers supply the property names.
        /// </summary>
        /// <param name="propertyNames">The ordered property names to look up and join.</param>
        public string BuildScopePath(IReadOnlyCollection<string> propertyNames)
        {
            var parts = propertyNames.Select(logEvent.GetScalarValue).Where(v => v != null);
            return string.Join('/', parts);
        }
    }
}
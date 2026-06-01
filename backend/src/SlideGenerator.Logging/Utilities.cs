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
    /// <summary>
    ///     Reads a scalar Serilog property value as text.
    /// </summary>
    /// <param name="logEvent">The event containing structured properties.</param>
    /// <param name="propertyName">The name of the scalar property to read.</param>
    /// <returns>The scalar value as text, or <see langword="null" /> when it is missing or not scalar.</returns>
    public static string? GetScalarValue(this LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value) &&
               value is ScalarValue { Value: not null } scalar
            ? scalar.Value.ToString()
            : null;
    }
}
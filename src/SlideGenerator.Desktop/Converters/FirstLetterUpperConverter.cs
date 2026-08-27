/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: FirstLetterUpperConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using Avalonia.Data.Converters;

namespace SlideGenerator.Desktop.Converters;

/// <summary>
///     Extracts the first character of a string, upper-cased — the initials-circle placeholder for a
///     Developer/Supporter row (plan §5.7: "avatar = initials circle") — used unconditionally rather than
///     only as an offline fallback, since downloading and caching real avatar images is its own subsystem
///     this phase doesn't build (<c>ponytail:</c> add real avatars if this is ever felt as a real gap).
/// </summary>
public sealed class FirstLetterUpperConverter : IValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly FirstLetterUpperConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string { Length: > 0 } s ? char.ToUpperInvariant(s[0]).ToString() : "?";
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

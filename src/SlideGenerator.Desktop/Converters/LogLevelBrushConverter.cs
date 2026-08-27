/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: LogLevelBrushConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SlideGenerator.Desktop.Converters;

/// <summary>
///     Colors a log line by its 3-letter level abbreviation (<c>"WRN"</c>/<c>"ERR"</c>, see
///     <c>FileLogFormatter</c>) — plan §5.4: "Log pane: ... màu WRN/ERR". Anything else (typically
///     <c>"INF"</c>) keeps the default text color by returning <see langword="null" />, letting the bound
///     property fall through to its own default rather than this converter hardcoding a "normal" brush.
/// </summary>
public sealed class LogLevelBrushConverter : IValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly LogLevelBrushConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = (value as string) switch
        {
            "WRN" => "WarningBrush",
            "ERR" => "DangerBrush",
            _ => null
        };
        return key is not null && Application.Current!.TryGetResource(key, Application.Current.ActualThemeVariant, out var brush)
            ? brush as IBrush
            : null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: BindingSummaryConverter.cs
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
///     Formats a <see cref="ValueTuple{T1,T2,T3,T4}" /> of (Assigned, Suggested, NeedsSelection, Unassigned)
///     counts — <c>TextBindingsViewModel.Summary</c>/<c>SlideCanvasViewModel</c>'s equivalent — into plan
///     §5.2's "12 đã ghép · 3 là đề xuất · 2 cần bạn chọn · 1 chưa gán" summary line.
/// </summary>
public sealed class BindingSummaryConverter : IValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly BindingSummaryConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not (int assigned, int suggested, int needsSelection, int unassigned)) return null;
        return $"{assigned} đã ghép · {suggested} là đề xuất · {needsSelection} cần bạn chọn · {unassigned} chưa gán";
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

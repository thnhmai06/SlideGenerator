/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: EnumEqualsConverter.cs
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
///     Compares a bound enum value against <c>ConverterParameter</c> — <c>true</c> when equal. Used for
///     highlighting the active choice in a chip/radio group without a boolean property per enum value (see
///     Runs' status filter, Settings' theme picker). <c>ConvertBack</c> lets the same binding drive a
///     two-way toggle (e.g. a <c>RadioButton.IsChecked</c>) that sets the source property to the parameter
///     value when checked.
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly EnumEqualsConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null && parameter is not null && value.Equals(parameter);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? parameter : Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>The negation of <see cref="EnumEqualsConverter" /> — <c>true</c> when the bound value differs from <c>ConverterParameter</c>.</summary>
public sealed class EnumNotEqualsConverter : IValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly EnumNotEqualsConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null && parameter is not null && !value.Equals(parameter);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

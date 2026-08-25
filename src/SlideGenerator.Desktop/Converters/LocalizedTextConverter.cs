/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: LocalizedTextConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using Avalonia.Data.Converters;
using SlideGenerator.Desktop.Services.Localization;

namespace SlideGenerator.Desktop.Converters;

/// <summary>
///     Ignores the bound value entirely and looks up <c>ConverterParameter</c> as a resource key via
///     <see cref="LocalizationService.Instance" />. Used by <see cref="TrExtension" />, which binds to
///     <see cref="ILocalizationService.Revision" /> (a plain named property) purely to trigger this convert
///     call on every <see cref="ILocalizationService.SetLanguage" /> — see <see cref="TrExtension" />'s doc
///     comment for why an indexer binding does not live-refresh in this app's compiled-binding pipeline.
/// </summary>
public sealed class LocalizedTextConverter : IValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly LocalizedTextConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return parameter is string key ? LocalizationService.Instance[key] : null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: EnumLocalizedTextConverter.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using SlideGenerator.Desktop.Services.Localization;

namespace SlideGenerator.Desktop.Converters;

/// <summary>
///     Localizes an enum value via <c>enums.{ConverterParameter}.{camelCasedMemberName}</c> resource keys (e.g.
///     <c>JobStatus.Running</c> with parameter <c>"jobStatus"</c> → <c>enums.jobStatus.running</c>). Takes a
///     <see cref="Avalonia.Data.MultiBinding" /> of (the enum value, <see cref="ILocalizationService.Revision" />)
///     rather than a plain <see cref="IValueConverter" /> — the same live-refresh requirement <see cref="TrExtension" />
///     solves by binding to <see cref="ILocalizationService.Revision" />: the enum value itself does not change when
///     <see cref="ILocalizationService.SetLanguage" /> is called, so a plain single-value converter would never
///     re-run on a language switch.
/// </summary>
public sealed class EnumLocalizedTextConverter : IMultiValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly EnumLocalizedTextConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not { } enumValue || parameter is not string area) return null;
        var name = enumValue.ToString()!;
        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        return LocalizationService.Instance[$"enums.{area}.{camel}"];
    }
}

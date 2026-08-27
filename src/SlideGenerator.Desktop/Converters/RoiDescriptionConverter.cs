/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RoiDescriptionConverter.cs
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
using SlideGenerator.Image.Cropping;

namespace SlideGenerator.Desktop.Converters;

/// <summary>
///     Explains one <see cref="RoiOption" /> in plain language (plan §5.3, image 26: "ROI cần giải thích trực
///     quan trong inspector" — resolved as short text rather than a drawn illustration) — e.g. an
///     <see cref="AnchorOption" /> with <see cref="AnchorType.Face" /> reads "Anchor point: Face center". Takes
///     a <see cref="Avalonia.Data.MultiBinding" /> of (the <see cref="RoiOption" />,
///     <see cref="ILocalizationService.Revision" />) for the same live-refresh reason as
///     <see cref="EnumLocalizedTextConverter" /> — the bound object doesn't change on a language switch, only
///     the strings looked up from it do.
/// </summary>
public sealed class RoiDescriptionConverter : IMultiValueConverter
{
    /// <summary>Gets the shared instance — this converter has no state, so one instance serves the whole app.</summary>
    public static readonly RoiDescriptionConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var loc = LocalizationService.Instance;
        return values.Count > 0 ? values[0] switch
        {
            AnchorOption a => $"{loc["enums.roiMode.anchor"]}: {loc[$"enums.anchorType.{Camel(a.Type.ToString())}"]}",
            InterestOption i => $"{loc["enums.roiMode.interest"]}: {loc[$"enums.interestType.{Camel(i.Type.ToString())}"]}",
            _ => ""
        } : "";
    }

    private static string Camel(string name)
    {
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}

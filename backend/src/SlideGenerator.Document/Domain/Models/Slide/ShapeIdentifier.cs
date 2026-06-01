/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: ShapeIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.ComponentModel;
using System.Globalization;

namespace SlideGenerator.Document.Domain.Models.Slide;

/// <summary>
///     Uniquely identifies a specific shape within a PowerPoint slide.
/// </summary>
/// <param name="PresentationPath">The path to the presentation.</param>
/// <param name="SlideIndex">The 1-based index of the slide.</param>
/// <param name="ShapeName">The unique name of the shape (e.g., "Rectangle 1").</param>
/// <param name="PresentationPassword">Optional password for the presentation.</param>
[TypeConverter(typeof(ShapeIdentifierConverter))]
public record ShapeIdentifier(
    string PresentationPath,
    int SlideIndex,
    string ShapeName,
    string? PresentationPassword = null)
    : SlideIdentifier(PresentationPath, SlideIndex, PresentationPassword);

/// <summary>
///     Converts <see cref="ShapeIdentifier" /> to/from a stable string key for JSON dictionary serialization.
///     Fields are delimited by ASCII Record Separator (0x1E), which never appears in file paths, indices, or passwords.
/// </summary>
public sealed class ShapeIdentifierConverter : TypeConverter
{
    private const char Sep = '\x1E';

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string s) return base.ConvertFrom(context, culture, value);
        var parts = s.Split(Sep);
        return new ShapeIdentifier(
            parts[0],
            parts.Length > 1 && int.TryParse(parts[1], out var idx) ? idx : 1,
            parts.Length > 2 ? parts[2] : string.Empty,
            parts.Length > 3 && parts[3].Length > 0 ? parts[3] : null);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
        Type destinationType)
    {
        if (destinationType != typeof(string) || value is not ShapeIdentifier id)
            return base.ConvertTo(context, culture, value, destinationType);
        return
            $"{id.PresentationPath}{Sep}{id.SlideIndex}{Sep}{id.ShapeName}{Sep}{id.PresentationPassword ?? string.Empty}";
    }
}
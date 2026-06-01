/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: SheetIdentifier.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.ComponentModel;
using System.Globalization;

namespace SlideGenerator.Document.Domain.Models.Sheet;

/// <summary>
///     Uniquely identifies a specific worksheet within an Excel workbook.
/// </summary>
/// <param name="BookPath">The path to the workbook.</param>
/// <param name="SheetName">The name of the worksheet.</param>
/// <param name="BookPassword">Optional password for the workbook.</param>
[TypeConverter(typeof(SheetIdentifierConverter))]
public record SheetIdentifier(string BookPath, string SheetName, string? BookPassword = null)
    : BookIdentifier(BookPath, BookPassword);

/// <summary>
///     Converts <see cref="SheetIdentifier" /> to/from a stable string key for JSON dictionary serialization.
///     Fields are delimited by ASCII Record Separator (0x1E), which never appears in file paths or passwords.
/// </summary>
public sealed class SheetIdentifierConverter : TypeConverter
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
        return new SheetIdentifier(
            parts[0],
            parts.Length > 1 ? parts[1] : string.Empty,
            parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null);
    }

    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
        Type destinationType)
    {
        if (destinationType != typeof(string) || value is not SheetIdentifier id)
            return base.ConvertTo(context, culture, value, destinationType);
        return $"{id.BookPath}{Sep}{id.SheetName}{Sep}{id.BookPassword ?? string.Empty}";
    }
}
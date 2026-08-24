/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: TrExtension.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace SlideGenerator.Desktop.Services.Localization;

/// <summary>
///     XAML markup extension for localized text: <c>Text="{loc:Tr AppTitle}"</c>. Binds to
///     <see cref="LocalizationService.Instance" />'s indexer rather than resolving a static string once, so
///     every use updates live when <see cref="ILocalizationService.SetLanguage" /> is called — a plain
///     <c>{x:Static}</c> lookup could not do this since <c>.resx</c> access has no change notification of its
///     own.
/// </summary>
public sealed class TrExtension(string key) : MarkupExtension
{
    /// <summary>Gets the resource key to look up.</summary>
    public string Key { get; } = key;

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };
    }
}

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
using SlideGenerator.Desktop.Converters;

namespace SlideGenerator.Desktop.Services.Localization;

/// <summary>
///     XAML markup extension for localized text: <c>Text="{loc:Tr AppTitle}"</c>. Binds to
///     <see cref="LocalizationService.Instance" />'s <see cref="ILocalizationService.Revision" /> property and
///     runs the actual resource lookup through <see cref="LocalizedTextConverter" /> (parameterized with
///     <see cref="Key" />) rather than resolving a static string once, so every use updates live when
///     <see cref="ILocalizationService.SetLanguage" /> is called - a plain <c>{x:Static}</c> lookup could not
///     do this since <c>.resx</c> access has no change notification of its own.
///     <para>
///     History (found during P5 Settings smoke-testing, this app's compiled-binding pipeline uses
///     <c>x:DataType</c> everywhere): the original design bound directly to an indexer
///     (<c>new Binding("[key]")</c>) against <see cref="LocalizationService" />'s <c>this[string]</c>. That
///     never live-refreshed no matter what <see cref="System.ComponentModel.PropertyChangedEventArgs.PropertyName" />
///     <see cref="LocalizationService.SetLanguage" /> raised. Returning a raw <see cref="IObservable{T}" />
///     from <see cref="ProvideValue" /> instead (bypassing <see cref="Binding" /> entirely) was tried next and
///     made it worse - the compiled-XAML setter cast the value directly rather than binding it, crashing every
///     view with a <c>{loc:Tr}</c> use at construction. The fix that actually works: bind to an ordinary named
///     property (<see cref="ILocalizationService.Revision" />, an <see langword="int" /> counter incremented
///     once per <see cref="ILocalizationService.SetLanguage" /> call) instead of an indexer path - this app's
///     pipeline already refreshes plain named-property bindings correctly everywhere else, the indexer path
///     specifically was the untested case. A <see cref="Binding.Converter" /> then ignores the bound
///     <see cref="ILocalizationService.Revision" /> value and re-looks-up <see cref="Key" /> against
///     <see cref="LocalizationService.Instance" /> on every change notification.
///     </para>
/// </summary>
public sealed class TrExtension(string key) : MarkupExtension
{
    /// <summary>Gets the resource key to look up.</summary>
    public string Key { get; } = key;

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(nameof(ILocalizationService.Revision))
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            Converter = LocalizedTextConverter.Instance,
            ConverterParameter = Key
        };
    }
}

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
///     every use updates live when <see cref="ILocalizationService.SetLanguage" /> is called - a plain
///     <c>{x:Static}</c> lookup could not do this since <c>.resx</c> access has no change notification of its
///     own.
///     <para>
///     Known gap (found during P5 Settings smoke-testing): this live refresh does not actually work in the
///     compiled-binding pipeline this app uses (<c>x:DataType</c> everywhere) - an indexer <see cref="Binding" />
///     against a plain <see cref="System.ComponentModel.INotifyPropertyChanged" /> source never re-fires no
///     matter what <see cref="System.ComponentModel.PropertyChangedEventArgs.PropertyName" />
///     <see cref="LocalizationService.SetLanguage" /> raises (tried both <see langword="null" /> and
///     <see cref="string.Empty" />), even though the indexer itself always returns the correct string for the
///     culture in effect. Returning a raw <see cref="IObservable{T}" /> from <see cref="ProvideValue" /> instead
///     (bypassing this accessor entirely) was tried and made it worse - the compiled-XAML setter cast the
///     value directly rather than binding it, crashing every view with a <c>{loc:Tr}</c> use at construction.
///     Reverted to this known-safe (if not live-refreshing) form. The saved language preference persists
///     correctly and takes effect on next launch; it does not update already-rendered text until then.
///     Fixing this properly needs a <see cref="Avalonia.Data.BindingBase" /> subclass wired through its
///     protected <c>CreateInstance</c>, which is a real (if narrow) piece of work - flagged for a follow-up
///     pass rather than attempted again here.
///     </para>
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

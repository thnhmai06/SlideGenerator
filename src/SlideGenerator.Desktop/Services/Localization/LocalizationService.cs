/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: LocalizationService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace SlideGenerator.Desktop.Services.Localization;

/// <summary>
///     Provides culture-aware string lookup that can change at runtime without an app restart. <c>.resx</c>
///     files (<c>Resources.resx</c>/<c>Resources.vi.resx</c>) are static and never raise change notifications
///     on their own — this service wraps them behind an indexer so <see cref="TrExtension" /> can bind to it
///     and refresh every bound view once <see cref="SetLanguage" /> is called.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>Gets the localized string for <paramref name="key" />, or <paramref name="key" /> itself if missing.</summary>
    string this[string key] { get; }

    /// <summary>Gets the culture currently in effect.</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    ///     Switches the active language and notifies every indexer binding to refresh.
    /// </summary>
    /// <param name="languageCode">
    ///     A culture name (e.g. <c>"vi"</c>, <c>"en"</c>), or empty to follow
    ///     <see cref="CultureInfo.CurrentUICulture" />.
    /// </param>
    void SetLanguage(string languageCode);
}

/// <summary>
///     Default <see cref="ILocalizationService" /> backed by the <c>Resources</c> <c>.resx</c> family.
///     Registered as a DI singleton (see <see cref="Registration" />); <see cref="TrExtension" /> reaches it
///     via <see cref="Instance" /> since XAML markup extensions are constructed by the XAML parser, not DI.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly ResourceManager ResourceManager =
        new("SlideGenerator.Desktop.Services.Localization.Resources", typeof(LocalizationService).Assembly);

    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    /// <summary>
    ///     Gets the process-wide instance, set once by the constructor since this service is registered as a
    ///     DI singleton — <see cref="TrExtension" /> cannot resolve it through DI, only through this static
    ///     reference.
    /// </summary>
    public static ILocalizationService Instance { get; private set; } = null!;

    /// <summary>Constructs the service and publishes it to <see cref="Instance" />.</summary>
    public LocalizationService()
    {
        Instance = this;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public string this[string key] => ResourceManager.GetString(key, _currentCulture) ?? key;

    /// <inheritdoc />
    public CultureInfo CurrentCulture => _currentCulture;

    /// <inheritdoc />
    public void SetLanguage(string languageCode)
    {
        _currentCulture = string.IsNullOrWhiteSpace(languageCode)
            ? CultureInfo.CurrentUICulture
            : new CultureInfo(languageCode);
        CultureInfo.CurrentUICulture = _currentCulture;
        // Null property name is the INotifyPropertyChanged convention for "every property changed" —
        // the only way to refresh every indexer ([key]) binding in the app from one call. In practice this
        // does not actually refresh already-rendered Avalonia compiled-binding indexer bindings — see
        // TrExtension's doc comment for the known gap this exposed.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}

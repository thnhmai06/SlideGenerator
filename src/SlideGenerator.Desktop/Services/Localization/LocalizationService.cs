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
///     on their own — <see cref="TrExtension" /> binds to <see cref="Revision" /> (a plain named property)
///     purely to trigger a re-lookup of the indexer through a converter once <see cref="SetLanguage" /> is
///     called; binding to the indexer directly does not live-refresh in this app's compiled-binding pipeline
///     (see <see cref="TrExtension" />'s doc comment).
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>Gets the localized string for <paramref name="key" />, or <paramref name="key" /> itself if missing.</summary>
    string this[string key] { get; }

    /// <summary>Gets the culture currently in effect.</summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>Gets a counter incremented once per <see cref="SetLanguage" /> call — <see cref="TrExtension" />
    ///     binds to this plain named property instead of the <c>[key]</c> indexer (see its own doc comment for
    ///     why), since an ordinary property-changed notification is the one refresh mechanism already known to
    ///     work through this app's compiled-binding pipeline.</summary>
    int Revision { get; }

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
    private int _revision;

    private static ILocalizationService? _instance;

    /// <summary>
    ///     Gets the process-wide instance, normally set once by the constructor since this service is
    ///     registered as a DI singleton — <see cref="TrExtension" /> cannot resolve it through DI, only through
    ///     this static reference. Self-initializes on first access if DI never constructed one (e.g. a
    ///     ViewModel unit test that calls <c>LocalizationService.Instance[key]</c> directly without going
    ///     through the app's host) — falling back to <see cref="CultureInfo.CurrentUICulture" /> is the same
    ///     default the real constructor would have used anyway.
    /// </summary>
    public static ILocalizationService Instance => _instance ??= new LocalizationService();

    /// <summary>Constructs the service and publishes it to <see cref="Instance" />.</summary>
    public LocalizationService()
    {
        _instance = this;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public string this[string key] => ResourceManager.GetString(key, _currentCulture) ?? key;

    /// <inheritdoc />
    public CultureInfo CurrentCulture => _currentCulture;

    /// <inheritdoc />
    public int Revision => _revision;

    /// <inheritdoc />
    public void SetLanguage(string languageCode)
    {
        _currentCulture = string.IsNullOrWhiteSpace(languageCode)
            ? CultureInfo.CurrentUICulture
            : new CultureInfo(languageCode);
        CultureInfo.CurrentUICulture = _currentCulture;
        _revision++;
        // Null property name still covers the (non-live-refreshing) indexer binding for any lingering
        // direct [key] usages; Revision is the one TrExtension actually relies on to live-refresh.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Revision)));
    }
}

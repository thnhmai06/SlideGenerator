/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: SettingsViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Bootstrap;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Theme;
using SlideGenerator.Settings.Mutable;

namespace SlideGenerator.Desktop.Features.Settings.ViewModels;

/// <summary>
///     Backs the one-scroll, three-group Settings page (plan §5.5: Giao diện/Hiệu năng/Mạng + a closing "Giới
///     thiệu" block). Every field persists immediately through <see cref="ISettingManager" /> on change — same
///     no-debounce, no-explicit-save convention <see cref="IThemeService.SetThemeAsync" /> already established
///     for Theme; a Settings page has no "Lưu" button anywhere in the plan. <c>ponytail:</c> a proxy/retry
///     text field writes to disk on every keystroke rather than debouncing — the settings file is tiny and
///     typing pace is not a throughput concern; add a debounce only if this is ever actually observed to lag.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingManager _settingManager;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;

    // True only while LoadFromSettings() is assigning the bound properties from a freshly (re)read Setting —
    // every OnXxxChanged hook must skip persisting in that window, or loading would immediately re-save the
    // same values right back (harmless but wasteful) and, worse, ThemeService.SetThemeAsync/
    // ILocalizationService.SetLanguage would re-apply on every load instead of only on a real user edit.
    private bool _isLoading;

    [ObservableProperty] private ThemeMode _theme;
    [ObservableProperty] private string _language = "";
    [ObservableProperty] private bool _reducedMotion;
    [ObservableProperty] private uint _maxConcurrentJobs;
    [ObservableProperty] private bool _useProxy;
    [ObservableProperty] private string _proxyAddress = "";
    [ObservableProperty] private string _proxyUsername = "";
    [ObservableProperty] private string _proxyPassword = "";
    [ObservableProperty] private string _proxyDomain = "";
    [ObservableProperty] private int _maxRetries;
    [ObservableProperty] private int _retryTimeoutSeconds;
    [ObservableProperty] private int _maxRetryDelaySeconds;
    [ObservableProperty] private uint _maxDownloadMegabytes;
    [ObservableProperty] private string? _updateStatusMessage;
    [ObservableProperty] private bool _isCheckingForUpdate;

    /// <summary>Gets the running app's informational version, for the "Giới thiệu" block.</summary>
    public string Version => Metadata.Value.Version;

    /// <summary>Gets the copyright/license line, for the "Giới thiệu" block.</summary>
    public string LicenseText => Metadata.Print.License;

    /// <summary>Gets the repository URL, for the "Giới thiệu" block.</summary>
    public string RepositoryUrl => Metadata.Value.Repository;

    /// <summary>Constructs the page and loads the current settings into its bound properties.</summary>
    public SettingsViewModel(ISettingManager settingManager, IThemeService themeService, ILocalizationService localizationService)
    {
        _settingManager = settingManager;
        _themeService = themeService;
        _localizationService = localizationService;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        _isLoading = true;
        try
        {
            var setting = _settingManager.Current;
            Theme = setting.Appearance.Theme;
            Language = setting.Appearance.Language;
            ReducedMotion = setting.Appearance.ReducedMotion;
            MaxConcurrentJobs = setting.Performance.MaxConcurrentJobs;
            UseProxy = setting.Network.Proxy.UseProxy;
            ProxyAddress = setting.Network.Proxy.ProxyAddress;
            ProxyUsername = setting.Network.Proxy.Username;
            ProxyPassword = setting.Network.Proxy.Password;
            ProxyDomain = setting.Network.Proxy.Domain;
            MaxRetries = setting.Network.Retry.MaxRetries;
            RetryTimeoutSeconds = setting.Network.Retry.Timeout;
            MaxRetryDelaySeconds = setting.Network.Retry.MaxRetryDelay;
            MaxDownloadMegabytes = setting.Network.MaxDownloadBytes / (1024 * 1024);
        }
        finally
        {
            _isLoading = false;
        }
    }

    partial void OnThemeChanged(ThemeMode value)
    {
        if (_isLoading) return;
        _ = _themeService.SetThemeAsync(value);
    }

    partial void OnLanguageChanged(string value)
    {
        if (_isLoading) return;
        _localizationService.SetLanguage(value);
        _ = PersistAsync();
    }

    partial void OnReducedMotionChanged(bool value)
    {
        if (_isLoading) return;
        Persist();
        _themeService.ApplyFromSettings();
    }
    partial void OnMaxConcurrentJobsChanged(uint value) => Persist();
    partial void OnUseProxyChanged(bool value) => Persist();
    partial void OnProxyAddressChanged(string value) => Persist();
    partial void OnProxyUsernameChanged(string value) => Persist();
    partial void OnProxyPasswordChanged(string value) => Persist();
    partial void OnProxyDomainChanged(string value) => Persist();
    partial void OnMaxRetriesChanged(int value) => Persist();
    partial void OnRetryTimeoutSecondsChanged(int value) => Persist();
    partial void OnMaxRetryDelaySecondsChanged(int value) => Persist();
    partial void OnMaxDownloadMegabytesChanged(uint value) => Persist();

    private void Persist()
    {
        if (_isLoading) return;
        _ = PersistAsync();
    }

    private async Task PersistAsync()
    {
        var current = _settingManager.Current;
        var updated = current with
        {
            Appearance = current.Appearance with { Theme = Theme, Language = Language, ReducedMotion = ReducedMotion },
            Performance = current.Performance with { MaxConcurrentJobs = MaxConcurrentJobs },
            Network = current.Network with
            {
                Proxy = current.Network.Proxy with
                {
                    UseProxy = UseProxy, ProxyAddress = ProxyAddress, Username = ProxyUsername,
                    Password = ProxyPassword, Domain = ProxyDomain
                },
                Retry = current.Network.Retry with
                {
                    MaxRetries = MaxRetries, Timeout = RetryTimeoutSeconds, MaxRetryDelay = MaxRetryDelaySeconds
                },
                MaxDownloadBytes = MaxDownloadMegabytes * 1024 * 1024
            }
        };
        await _settingManager.Update(updated).ConfigureAwait(true);
    }

    /// <summary>Resets only the Hiệu năng group to defaults (plan §5.5's per-group "Khôi phục mặc định" — not
    ///     a whole-app reset, which would also discard Giao diện/Mạng).</summary>
    [RelayCommand]
    private async Task ResetPerformanceAsync()
    {
        var current = _settingManager.Current;
        await _settingManager.Update(current with { Performance = new Setting.PerformanceSetting() }).ConfigureAwait(true);
        LoadFromSettings();
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        IsCheckingForUpdate = true;
        UpdateStatusMessage = null;
        try
        {
            var result = await UpdateChecker.CheckForUpdatesAsync().ConfigureAwait(true);
            UpdateStatusMessage = result switch
            {
                UpdateCheckResult.NotInstalled => "Không áp dụng. Ứng dụng không chạy dưới dạng bản cài đặt.",
                UpdateCheckResult.UpToDate => "Đã là bản mới nhất.",
                UpdateCheckResult.UpdateDownloaded => "Đã tải bản cập nhật. Khởi động lại để áp dụng.",
                UpdateCheckResult.Failed => "Kiểm tra cập nhật thất bại. Thử lại sau.",
                _ => null
            };
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }
}

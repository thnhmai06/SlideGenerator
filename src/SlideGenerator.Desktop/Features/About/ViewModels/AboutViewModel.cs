/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: AboutViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Bootstrap;
using SlideGenerator.Desktop.Features.About.Models;
using SlideGenerator.Desktop.Features.About.Services;
using SlideGenerator.Desktop.Services.Localization;

namespace SlideGenerator.Desktop.Features.About.ViewModels;

/// <summary>
///     Backs the About page (plan §5.7): brand replay, description, update check (moved here from Settings'
///     old closing "Giới thiệu" block), live Developers/Supporters lists, and the repository/sponsor links.
///     Registered as a DI singleton — <see cref="LoadAsync" /> only runs once per app session, the first time
///     the page is opened (plan: "fetch lazy lần đầu mở"), not on every navigation back to it.
/// </summary>
public sealed partial class AboutViewModel : ObservableObject
{
    private readonly IAboutDataService _dataService;
    private bool _loaded;

    [ObservableProperty] private bool _isLoadingDevelopers = true;
    [ObservableProperty] private bool _isLoadingSupporters = true;
    [ObservableProperty] private string? _updateStatusMessage;
    [ObservableProperty] private bool _isCheckingForUpdate;

    /// <summary>Gets the running app's informational version.</summary>
    public string Version => Metadata.Value.Version;

    /// <summary>Gets the copyright/license line.</summary>
    public string LicenseText => Metadata.Print.License;

    /// <summary>Gets the repository URL, shown as a link.</summary>
    public string RepositoryUrl => Metadata.Value.Repository;

    /// <summary>Gets the English tagline. Shown together with <see cref="DescriptionVi" /> (plan §5.7: "hiển
    ///     thị cả 2 dòng như idea") — deliberately not run through <c>{loc:Tr}</c>, since both lines always
    ///     appear together regardless of the active UI language rather than switching with it.</summary>
    public string DescriptionEn => Metadata.Print.Description;

    /// <summary>Gets the Vietnamese tagline — see <see cref="DescriptionEn" />.</summary>
    public string DescriptionVi => "Công cụ tự động tạo bài trình chiếu từ mẫu.";

    /// <summary>Gets every contributor to the repository — empty until <see cref="LoadAsync" /> completes.</summary>
    public ObservableCollection<Contributor> Developers { get; } = [];

    /// <summary>Gets every current GitHub Sponsor — empty until <see cref="LoadAsync" /> completes, or if there
    ///     are none yet (drives the Supporters empty state, not an error).</summary>
    public ObservableCollection<Supporter> Supporters { get; } = [];

    /// <summary>Gets whether <see cref="Supporters" /> has at least one entry.</summary>
    public bool HasSupporters => Supporters.Count > 0;

    /// <summary>Gets whether <see cref="Developers" /> has at least one entry.</summary>
    public bool HasDevelopers => Developers.Count > 0;

    /// <summary>Constructs the ViewModel. Data loading is deferred to <see cref="LoadAsync" />.</summary>
    public AboutViewModel(IAboutDataService dataService)
    {
        _dataService = dataService;
    }

    /// <summary>Fetches Developers/Supporters once per app session — safe to call every time the page is
    ///     shown, a no-op after the first successful call.</summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;

        IsLoadingDevelopers = true;
        try
        {
            foreach (var c in await _dataService.GetContributorsAsync().ConfigureAwait(true)) Developers.Add(c);
            OnPropertyChanged(nameof(HasDevelopers));
        }
        finally
        {
            IsLoadingDevelopers = false;
        }

        IsLoadingSupporters = true;
        try
        {
            foreach (var s in await _dataService.GetSupportersAsync().ConfigureAwait(true)) Supporters.Add(s);
            OnPropertyChanged(nameof(HasSupporters));
        }
        finally
        {
            IsLoadingSupporters = false;
        }
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
                UpdateCheckResult.NotInstalled => LocalizationService.Instance["settings.about.updateStatus.notInstalled"],
                UpdateCheckResult.UpToDate => LocalizationService.Instance["settings.about.updateStatus.upToDate"],
                UpdateCheckResult.UpdateDownloaded => LocalizationService.Instance["settings.about.updateStatus.downloaded"],
                UpdateCheckResult.Failed => LocalizationService.Instance["settings.about.updateStatus.failed"],
                _ => null
            };
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }

    [RelayCommand]
    private static void OpenRepository()
    {
        OpenUrl(Metadata.Value.Repository);
    }

    /// <summary>Opens the sponsor page for the one GitHub profile <c>.github/FUNDING.yml</c> currently lists
    ///     (<c>thnhmai06</c>) — a flyout to choose between multiple profiles is only needed once a second
    ///     profile is ever added there (plan §5.7).</summary>
    [RelayCommand]
    private static void OpenSponsorPage()
    {
        OpenUrl("https://github.com/sponsors/thnhmai06");
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}

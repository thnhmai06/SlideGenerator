/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: MainWindowViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SlideGenerator.Desktop.Services.Localization;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Generator.Jobs.Models;

namespace SlideGenerator.Desktop.Shell;

/// <summary>
///     Owns the single top-level content swap for the app's one window: <see cref="SplashViewModel" /> while
///     startup init is still running, then <see cref="ShellViewModel" /> once it completes (see
///     <c>App.axaml.cs</c>'s startup sequencing). A separate, tiny ViewModel rather than folding this into
///     <see cref="ShellViewModel" /> — splash lifecycle and page navigation are different concerns, and
///     <see cref="ShellViewModel" /> should not need to know it was ever preceded by a splash screen.
///     Also owns <see cref="WindowTitle" /> — cheap, v1-inherited polish: the taskbar/window title shows how
///     many jobs are currently active, visible without switching back to the app (blueprint §5.1).
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private const string BaseTitle = "SlideGenerator";

    private readonly ILocalizationService _localizationService;

    [ObservableProperty] private ObservableObject? _currentContent;
    [ObservableProperty] private string _windowTitle = BaseTitle;

    /// <summary>Constructs the VM and starts tracking <paramref name="progressHub" />'s jobs for the window title.</summary>
    /// <param name="progressHub">Source of truth for how many jobs are currently active.</param>
    /// <param name="localizationService">Localizes the "N job(s) running" suffix; re-applied on language change.</param>
    public MainWindowViewModel(IProgressHub progressHub, ILocalizationService localizationService)
    {
        _localizationService = localizationService;
        progressHub.Jobs.CollectionChanged += (_, _) => UpdateWindowTitle(progressHub.Jobs.Count(IsActive));
        localizationService.PropertyChanged += (_, _) => UpdateWindowTitle(progressHub.Jobs.Count(IsActive));
    }

    private static bool IsActive(JobSnapshot job)
    {
        return job.JobStatus is JobStatus.Pending or JobStatus.Running;
    }

    private void UpdateWindowTitle(int activeCount)
    {
        WindowTitle = activeCount > 0
            ? BaseTitle + string.Format(_localizationService["ShellActiveJobsTitleSuffix"], activeCount)
            : BaseTitle;
    }
}

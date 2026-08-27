/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RunsViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideGenerator.Desktop.Features.Runs.Models;
using SlideGenerator.Desktop.Services.Dialogs;
using SlideGenerator.Desktop.Services.Progress;
using SlideGenerator.Generator;
using SlideGenerator.Generator.Jobs.Models;
using SlideGenerator.Generator.Progress;

namespace SlideGenerator.Desktop.Features.Runs.ViewModels;

/// <summary>
///     Owns the single Runs list — <c>ListActiveAsync</c>/<c>ListCompletedAsync</c> return the identical
///     <see cref="Summary" /> shape, so "active" and "completed" are a chip filter over one list, not two
///     pages. After the initial load, per-job updates arrive live via <see cref="IProgressHub" /> and patch
///     existing rows directly — a brand-new request (a key this ViewModel has never seen) triggers one full
///     reload instead, since building its display requires <see cref="Summary" /> fields
///     (<c>Request.Name</c>, <c>CreatedAt</c>, …) a live <see cref="JobSnapshot" /> alone doesn't carry.
/// </summary>
public sealed partial class RunsViewModel : ObservableObject
{
    private readonly Dictionary<string, RequestRunViewModel> _byRequestId = new();
    private readonly IDialogService _dialogService;
    private readonly IProgressHub _progressHub;
    private readonly IService _service;

    [ObservableProperty] private RunStatusFilter _filter = RunStatusFilter.All;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private RequestRunViewModel? _selectedRequest;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>Gets the requests currently matching <see cref="Filter" />/<see cref="SearchText" />, newest first.</summary>
    public ObservableCollection<RequestRunViewModel> FilteredRequests { get; } = [];

    /// <summary>Gets whether <see cref="FilteredRequests" /> has at least one entry — drives the empty state.</summary>
    public bool HasResults => FilteredRequests.Count > 0;

    /// <summary>Constructs the ViewModel and starts the initial load.</summary>
    public RunsViewModel(IService service, IProgressHub progressHub, IDialogService dialogService)
    {
        _service = service;
        _progressHub = progressHub;
        _dialogService = dialogService;
        _progressHub.Jobs.CollectionChanged += OnJobsChanged;
        _progressHub.Rows.CollectionChanged += OnRowsChanged;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var active = await _service.ListActiveAsync(includeLogs: false).ConfigureAwait(true);
            var completed = await _service.ListCompletedAsync(includeLogs: false).ConfigureAwait(true);

            _byRequestId.Clear();
            foreach (var (requestId, summary) in active.Concat(completed))
                _byRequestId[requestId] = new RequestRunViewModel(requestId, summary, _service, _dialogService);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Reloads and selects <paramref name="requestId" /> — used when navigating here right after a run starts.</summary>
    public async Task SelectRequestAsync(string requestId)
    {
        await LoadAsync().ConfigureAwait(true);
        if (_byRequestId.TryGetValue(requestId, out var request)) SelectedRequest = request;
    }

    partial void OnFilterChanged(RunStatusFilter value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedRequestChanged(RequestRunViewModel? value)
    {
        if (value is not null) _ = value.LoadLogsAsync();
    }

    private void ApplyFilter()
    {
        var previouslySelectedId = SelectedRequest?.RequestId;
        var matching = _byRequestId.Values
            .Where(MatchesFilter)
            .Where(MatchesSearch)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        FilteredRequests.Clear();
        foreach (var request in matching) FilteredRequests.Add(request);
        OnPropertyChanged(nameof(HasResults));

        SelectedRequest = previouslySelectedId is not null
            ? FilteredRequests.FirstOrDefault(r => r.RequestId == previouslySelectedId)
            : FilteredRequests.FirstOrDefault();
    }

    private bool MatchesFilter(RequestRunViewModel request)
    {
        return Filter switch
        {
            RunStatusFilter.Running => request.Status == JobStatus.Running,
            RunStatusFilter.Paused => request.Status == JobStatus.Paused,
            RunStatusFilter.Done => request.Status == JobStatus.Complete,
            RunStatusFilter.Cancelled => request.Status == JobStatus.Cancelled,
            _ => true
        };
    }

    private bool MatchesSearch(RequestRunViewModel request)
    {
        return string.IsNullOrWhiteSpace(SearchText) ||
               request.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void OnJobsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _ = LoadAsync();
            return;
        }

        if (e.NewItems is null) return;
        var sawUnknownRequest = false;
        foreach (JobSnapshot snapshot in e.NewItems)
            if (_byRequestId.TryGetValue(snapshot.RequestId, out var request))
                request.ApplyLiveJobUpdate(snapshot);
            else
                sawUnknownRequest = true;

        // A brand-new request's first job(s) arrived — reload once to pick it up with full Summary fields.
        if (sawUnknownRequest) _ = LoadAsync();
    }

    // Unlike OnJobsChanged, a row belonging to a request this VM hasn't loaded yet is simply dropped — its
    // job snapshot arrives via OnJobsChanged in the same batch and triggers the reload that will show it;
    // there is nothing row-specific to display until then.
    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null) return;
        foreach (RowProgress row in e.NewItems)
            if (_byRequestId.TryGetValue(row.RequestId, out var request))
                request.ApplyLiveRowUpdate(row);
    }
}

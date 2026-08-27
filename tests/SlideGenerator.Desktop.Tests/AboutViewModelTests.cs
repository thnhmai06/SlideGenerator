/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop.Tests
 * File: AboutViewModelTests.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using FluentAssertions;
using NSubstitute;
using SlideGenerator.Desktop.Features.About.Models;
using SlideGenerator.Desktop.Features.About.Services;
using SlideGenerator.Desktop.Features.About.ViewModels;
using Xunit;

namespace SlideGenerator.Desktop.Tests;

/// <summary>
///     Unit tests for <see cref="AboutViewModel.LoadAsync" /> against a mocked <see cref="IAboutDataService" />
///     (plan §5.7/§7.1: "About fetch+fallback" test) — populating Developers/Supporters, the once-per-session
///     guard, and that an empty result still surfaces the empty-state flags correctly rather than throwing.
/// </summary>
public sealed class AboutViewModelTests
{
    [Fact]
    public async Task LoadAsync_ContributorsAndSupportersReturned_PopulatesBothLists()
    {
        var dataService = Substitute.For<IAboutDataService>();
        dataService.GetContributorsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Contributor>)new List<Contributor> { new("thnhmai06", "https://a", "https://p", 42) });
        dataService.GetSupportersAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Supporter>)new List<Supporter> { new("fan1", "https://a2", "https://p2") });
        var vm = new AboutViewModel(dataService);

        await vm.LoadAsync();

        vm.Developers.Should().ContainSingle(d => d.Login == "thnhmai06");
        vm.Supporters.Should().ContainSingle(s => s.Login == "fan1");
        vm.HasDevelopers.Should().BeTrue();
        vm.HasSupporters.Should().BeTrue();
        vm.IsLoadingDevelopers.Should().BeFalse();
        vm.IsLoadingSupporters.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_NoSupporters_HasSupportersIsFalse()
    {
        var dataService = Substitute.For<IAboutDataService>();
        dataService.GetContributorsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Contributor>)new List<Contributor>());
        dataService.GetSupportersAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Supporter>)new List<Supporter>());
        var vm = new AboutViewModel(dataService);

        await vm.LoadAsync();

        vm.HasDevelopers.Should().BeFalse();
        vm.HasSupporters.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_CalledTwice_FetchesOnlyOnce()
    {
        var dataService = Substitute.For<IAboutDataService>();
        dataService.GetContributorsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Contributor>)new List<Contributor>());
        dataService.GetSupportersAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Supporter>)new List<Supporter>());
        var vm = new AboutViewModel(dataService);

        await vm.LoadAsync();
        await vm.LoadAsync();

        await dataService.Received(1).GetContributorsAsync(Arg.Any<CancellationToken>());
        await dataService.Received(1).GetSupportersAsync(Arg.Any<CancellationToken>());
    }
}

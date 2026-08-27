/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: IAboutDataService.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using SlideGenerator.Desktop.Bootstrap;
using SlideGenerator.Desktop.Features.About.Models;
using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Desktop.Features.About.Services;

/// <summary>
///     Fetches the About page's two live-data lists (plan §5.7): repository contributors from the GitHub
///     REST API, and sponsors from a <c>sponsors.json</c> file a scheduled GitHub Action publishes (see
///     <c>.github/workflows/sponsors.yml</c>) — GitHub Sponsors has no public unauthenticated REST endpoint,
///     so the app can't query it directly. Both calls are disk-cached (24h TTL) so the About page doesn't
///     re-fetch on every visit within the same day, and both fail soft to an empty list — a network problem
///     must never make the About page itself show an error (plan: "About không bao giờ lỗi đỏ").
/// </summary>
public interface IAboutDataService
{
    /// <summary>Gets every contributor to the repository, most contributions first, or an empty list if the
    ///     API is unreachable and no cache exists yet.</summary>
    Task<IReadOnlyList<Contributor>> GetContributorsAsync(CancellationToken ct = default);

    /// <summary>Gets every current GitHub Sponsor, or an empty list if <c>sponsors.json</c> doesn't exist yet
    ///     (no sponsors, or the publishing workflow hasn't run) or is unreachable.</summary>
    Task<IReadOnlyList<Supporter>> GetSupportersAsync(CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class AboutDataService : IAboutDataService
{
    private const string RepoOwner = "thnhmai06";
    private const string RepoName = "SlideGenerator";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse($"{RepoName}/{Metadata.Value.Version}") } }
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<Contributor>> GetContributorsAsync(CancellationToken ct = default)
    {
        return await FetchCachedAsync<IReadOnlyList<Contributor>>(
            "about-contributors.json",
            async () =>
            {
                var rows = await Http.GetFromJsonAsync<List<ContributorRow>>(
                    $"https://api.github.com/repos/{RepoOwner}/{RepoName}/contributors", ct).ConfigureAwait(false);
                return (IReadOnlyList<Contributor>)(rows ?? [])
                    .OrderByDescending(r => r.Contributions)
                    .Select(r => new Contributor(r.Login, r.AvatarUrl, r.HtmlUrl, r.Contributions))
                    .ToList();
            }, ct).ConfigureAwait(false) ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Supporter>> GetSupportersAsync(CancellationToken ct = default)
    {
        return await FetchCachedAsync<IReadOnlyList<Supporter>>(
            "about-sponsors.json",
            async () =>
            {
                // 404 (workflow hasn't run yet, or no sponsors) is expected, not an error — GetAsync below
                // treats it the same as any other failure: fall through to an empty list.
                var response = await Http.GetAsync(
                    $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/data/sponsors.json", ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return [];
                var rows = await response.Content.ReadFromJsonAsync<List<Supporter>>(ct).ConfigureAwait(false);
                return (IReadOnlyList<Supporter>)(rows ?? []);
            }, ct).ConfigureAwait(false) ?? [];
    }

    /// <summary>Returns the cached value at <paramref name="cacheFileName" /> if written within
    ///     <see cref="CacheTtl" />; otherwise calls <paramref name="fetch" />, caching a successful result. A
    ///     failed fetch falls back to a stale cache if one exists, then to <see langword="default" /> (an
    ///     empty list, from each caller's own <c>?? []</c>) — a fetch failure must never surface as an
    ///     exception to the ViewModel.</summary>
    private static async Task<T?> FetchCachedAsync<T>(string cacheFileName, Func<Task<T>> fetch, CancellationToken ct)
    {
        var path = Path.Combine(NameAndPaths.DataFolder.FolderPath, cacheFileName);
        var cached = TryReadCache<T>(path);
        if (cached is { Age: var age } && age < CacheTtl) return cached.Value.Value;

        try
        {
            var fresh = await fetch().ConfigureAwait(false);
            Directory.CreateDirectory(NameAndPaths.DataFolder.FolderPath);
            await File.WriteAllTextAsync(path,
                JsonSerializer.Serialize(new CacheEnvelope<T>(DateTimeOffset.UtcNow, fresh)), ct).ConfigureAwait(false);
            return fresh;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            Log.Warning(ex, "About page data fetch failed for {CacheFile}; falling back to stale/empty.", cacheFileName);
            return cached is { } stale ? stale.Value : default;
        }
    }

    private static (T Value, TimeSpan Age)? TryReadCache<T>(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var envelope = JsonSerializer.Deserialize<CacheEnvelope<T>>(File.ReadAllText(path));
            return envelope is null ? null : (envelope.Data, DateTimeOffset.UtcNow - envelope.FetchedAt);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null; // corrupt/unreadable cache — treat as no cache, re-fetch
        }
    }

    private sealed record CacheEnvelope<T>(DateTimeOffset FetchedAt, T Data);

    private sealed record ContributorRow(
        string Login,
        [property: JsonPropertyName("avatar_url")] string AvatarUrl,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        int Contributions);
}

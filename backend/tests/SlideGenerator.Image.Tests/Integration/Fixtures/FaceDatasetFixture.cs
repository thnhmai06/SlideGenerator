/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Image.Tests
 * File: FaceDatasetFixture.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */

using System.Text.Json;
using Xunit;

namespace SlideGenerator.Image.Tests.Integration.Fixtures;

/// <summary>
///     xUnit v3 collection fixture that downloads face test images from HuggingFace datasets-server
///     and caches them locally under <c>tests/fixtures/faces/</c>.
/// </summary>
/// <remarks>
///     <para>
///         <b>Single portraits</b> come from <c>eurecom-ds/celeba-hq</c> (30k × 1024×1024 images).
///         Gender balance (50 % male / 50 % female) is enforced by reading <c>attributes[20]</c>
///         — the standard CelebA "Male" attribute flag (1 = male, 0 = female).
///     </para>
///     <para>
///         <b>Group and crowd images</b> come from <c>NekoJojo/modified_wider_face_val</c> (3 167 images).
///         WIDER FACE carries no gender labels; 50/50 balance cannot be enforced for these categories.
///         Face count is encoded in the filename (e.g. <c>g005f_0.jpg</c> = 5 detected faces).
///     </para>
///     <para>
///         Cache semantics:
///         <list type="bullet">
///             <item>Cache full → tests run, no network call.</item>
///             <item>
///                 Cache partial → missing images are fetched; if the network is unavailable the fixture
///                 continues with what is already cached, and individual tests call
///                 <see cref="Assert.Skip" /> when their required category is empty.
///             </item>
///             <item>Cache empty + no network → all integration tests are skipped.</item>
///         </list>
///     </para>
/// </remarks>
public sealed class FaceDatasetFixture : IAsyncLifetime
{
    #region Constants

    /// <summary>Target number of male single-portrait images (CelebA-HQ).</summary>
    public const int SingleMaleTarget = 15;

    /// <summary>Target number of female single-portrait images (CelebA-HQ).</summary>
    public const int SingleFemaleTarget = 15;

    /// <summary>Target number of group images — 2 to <see cref="GroupMaxFaces" /> faces (WIDER FACE).</summary>
    public const int GroupTarget = 15;

    /// <summary>Target number of crowd images — more than <see cref="GroupMaxFaces" /> faces (WIDER FACE).</summary>
    public const int CrowdTarget = 5;

    private const int GroupMinFaces = 2;
    private const int GroupMaxFaces = 15;

    private const string CelebaDataset = "eurecom-ds%2Fceleba-hq";
    private const string CelebaSplit = "train";
    private const int MaleAttributeIndex = 20;
    private const int CelebaMaxScanRows = 2000;

    private const string WiderFaceDataset = "NekoJojo%2Fmodified_wider_face_val";
    private const string WiderFaceSplit = "validation";
    private const int WiderFaceTotalRows = 3200;

    private const int BatchSize = 50;

    #endregion

    #region Directories

    private static readonly string FixturesRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "faces"));

    /// <summary>Local directory for single-portrait images.</summary>
    public string SingleDir { get; } = Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "faces")),
        "single");

    /// <summary>Local directory for group images (2–15 faces).</summary>
    public string GroupDir { get; } = Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "faces")),
        "group");

    /// <summary>Local directory for crowd images (16+ faces).</summary>
    public string CrowdDir { get; } = Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "faces")),
        "crowd");

    #endregion

    #region IAsyncLifetime

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(SingleDir);
        Directory.CreateDirectory(GroupDir);
        Directory.CreateDirectory(CrowdDir);

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            await EnsureSingleAsync(http).ConfigureAwait(false);
        }
        catch
        {
            /* network unavailable — continue with cached images */
        }

        try
        {
            await EnsureGroupCrowdAsync(http).ConfigureAwait(false);
        }
        catch
        {
            /* network unavailable — continue with cached images */
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Public accessors

    /// <summary>Returns all cached single-portrait paths.</summary>
    public string[] GetSingleImages()
    {
        return Directory.GetFiles(SingleDir, "*.jpg");
    }

    /// <summary>Returns all cached group image paths.</summary>
    public string[] GetGroupImages()
    {
        return Directory.GetFiles(GroupDir, "*.jpg");
    }

    /// <summary>Returns all cached crowd image paths.</summary>
    public string[] GetCrowdImages()
    {
        return Directory.GetFiles(CrowdDir, "*.jpg");
    }

    #endregion

    #region Download helpers

    private async Task EnsureSingleAsync(HttpClient http)
    {
        var neededMale = SingleMaleTarget - Directory.GetFiles(SingleDir, "m_*.jpg").Length;
        var neededFemale = SingleFemaleTarget - Directory.GetFiles(SingleDir, "f_*.jpg").Length;
        if (neededMale <= 0 && neededFemale <= 0) return;

        for (var offset = 0; offset < CelebaMaxScanRows && (neededMale > 0 || neededFemale > 0); offset += BatchSize)
        {
            var url = BuildRowsUrl(CelebaDataset, CelebaSplit, offset, BatchSize);
            string json;
            try
            {
                json = await http.GetStringAsync(url).ConfigureAwait(false);
            }
            catch
            {
                break;
            }

            using var doc = JsonDocument.Parse(json);
            foreach (var entry in doc.RootElement.GetProperty("rows").EnumerateArray())
            {
                var rowIdx = entry.GetProperty("row_idx").GetInt32();
                var row = entry.GetProperty("row");
                var src = row.GetProperty("image").GetProperty("src").GetString()!;
                var attrs = row.GetProperty("attributes");
                var isMale = attrs[MaleAttributeIndex].GetInt32() == 1;

                if (isMale && neededMale > 0)
                {
                    var dest = Path.Combine(SingleDir, $"m_{rowIdx}.jpg");
                    if (!File.Exists(dest) && await TryDownloadAsync(http, src, dest).ConfigureAwait(false))
                        neededMale--;
                }
                else if (!isMale && neededFemale > 0)
                {
                    var dest = Path.Combine(SingleDir, $"f_{rowIdx}.jpg");
                    if (!File.Exists(dest) && await TryDownloadAsync(http, src, dest).ConfigureAwait(false))
                        neededFemale--;
                }

                if (neededMale <= 0 && neededFemale <= 0) break;
            }
        }
    }

    private async Task EnsureGroupCrowdAsync(HttpClient http)
    {
        var neededGroup = GroupTarget - Directory.GetFiles(GroupDir, "*.jpg").Length;
        var neededCrowd = CrowdTarget - Directory.GetFiles(CrowdDir, "*.jpg").Length;
        if (neededGroup <= 0 && neededCrowd <= 0) return;

        for (var offset = 0; offset < WiderFaceTotalRows && (neededGroup > 0 || neededCrowd > 0); offset += BatchSize)
        {
            var url = BuildRowsUrl(WiderFaceDataset, WiderFaceSplit, offset, BatchSize);
            string json;
            try
            {
                json = await http.GetStringAsync(url).ConfigureAwait(false);
            }
            catch
            {
                break;
            }

            using var doc = JsonDocument.Parse(json);
            foreach (var entry in doc.RootElement.GetProperty("rows").EnumerateArray())
            {
                var rowIdx = entry.GetProperty("row_idx").GetInt32();
                var row = entry.GetProperty("row");
                var src = row.GetProperty("image").GetProperty("src").GetString()!;
                var faceCount = row.GetProperty("valid_length").GetInt32();

                if (faceCount >= GroupMinFaces && faceCount <= GroupMaxFaces && neededGroup > 0)
                {
                    var dest = Path.Combine(GroupDir, $"g{faceCount:D3}f_{rowIdx}.jpg");
                    if (!File.Exists(dest) && await TryDownloadAsync(http, src, dest).ConfigureAwait(false))
                        neededGroup--;
                }
                else if (faceCount > GroupMaxFaces && neededCrowd > 0)
                {
                    var dest = Path.Combine(CrowdDir, $"c{faceCount:D3}f_{rowIdx}.jpg");
                    if (!File.Exists(dest) && await TryDownloadAsync(http, src, dest).ConfigureAwait(false))
                        neededCrowd--;
                }

                if (neededGroup <= 0 && neededCrowd <= 0) break;
            }
        }
    }

    private static string BuildRowsUrl(string dataset, string split, int offset, int length)
    {
        return $"https://datasets-server.huggingface.co/rows" +
               $"?dataset={dataset}&config=default&split={split}" +
               $"&offset={offset}&length={length}";
    }

    private static async Task<bool> TryDownloadAsync(HttpClient http, string url, string dest)
    {
        try
        {
            var bytes = await http.GetByteArrayAsync(url).ConfigureAwait(false);
            await File.WriteAllBytesAsync(dest, bytes).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
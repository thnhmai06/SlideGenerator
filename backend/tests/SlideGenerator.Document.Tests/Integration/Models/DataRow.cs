/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document.Tests
 * File: DataRow.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using Xunit.Sdk;

namespace SlideGenerator.Document.Tests.Integration.Models;

/// <summary>
///     Maps a single row from <c>Data.csv</c> for use in <c>TextComposer</c> integration tests.
///     All six columns are stored as strings and forwarded verbatim as resolved values to the composer.
///     Implements <see cref="IXunitSerializable" /> so xUnit can serialize and display each case by name.
/// </summary>
public sealed class DataRow : IXunitSerializable
{
    /// <summary>Gets or sets the cloud provider name (e.g. <c>Google Drive</c>, <c>Raw</c>).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable scenario description.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the resource should be downloaded (<c>TRUE</c> / <c>FALSE</c>).</summary>
    public string ShouldDownload { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw URL under test.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the pre-built JSON object string for <c>DataJson</c> placeholder.</summary>
    public string DataJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the Excel-array string for <c>DataList</c> placeholder.</summary>
    public string DataList { get; set; } = string.Empty;

    /// <summary>
    ///     Returns a file-system-safe name derived from <see cref="Provider" /> and <see cref="Type" />,
    ///     suitable for use as a PPTX output filename (without extension).
    /// </summary>
    public string SafeFileName
    {
        get
        {
            var invalid = Path.GetInvalidFileNameChars();
            return $"{Sanitize(Provider)}_{Sanitize(Type)}";
            string Sanitize(string s) => string.Concat(s.Select(c => invalid.Contains(c) ? '_' : c));
        }
    }

    /// <summary>
    ///     Builds the resolved-values dictionary passed to <c>TextComposer.Compose</c>.
    ///     All six CSV columns are included verbatim.
    /// </summary>
    public IReadOnlyDictionary<string, string> ToResolvedValues() =>
        new Dictionary<string, string>
        {
            ["Provider"]       = Provider,
            ["Type"]           = Type,
            ["ShouldDownload"] = ShouldDownload,
            ["Url"]            = Url,
            ["DataJson"]       = DataJson,
            ["DataList"]       = DataList,
        };

    /// <inheritdoc />
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Provider), Provider);
        info.AddValue(nameof(Type), Type);
        info.AddValue(nameof(ShouldDownload), ShouldDownload);
        info.AddValue(nameof(Url), Url);
        info.AddValue(nameof(DataJson), DataJson);
        info.AddValue(nameof(DataList), DataList);
    }

    /// <inheritdoc />
    public void Deserialize(IXunitSerializationInfo info)
    {
        Provider       = info.GetValue<string>(nameof(Provider))       ?? string.Empty;
        Type           = info.GetValue<string>(nameof(Type))           ?? string.Empty;
        ShouldDownload = info.GetValue<string>(nameof(ShouldDownload)) ?? string.Empty;
        Url            = info.GetValue<string>(nameof(Url))            ?? string.Empty;
        DataJson       = info.GetValue<string>(nameof(DataJson))       ?? string.Empty;
        DataList       = info.GetValue<string>(nameof(DataList))       ?? string.Empty;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Provider} / {Type}";
}

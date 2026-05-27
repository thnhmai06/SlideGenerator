/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Cloud.Tests
 * File: TestCase.cs
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

using Xunit.Sdk;

namespace SlideGenerator.Cloud.Tests.Integration.Models;

/// <summary>
///     Maps a single row from <c>Testcases.csv</c> used in integration tests.
///     Implements <see cref="IXunitSerializable" /> so xUnit can serialize and display each test case
///     by name in the test runner.
/// </summary>
public sealed class TestCase : IXunitSerializable
{
    /// <summary>Gets or sets the cloud provider name (e.g. <c>Google Drive</c>, <c>Raw</c>).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable description of the test scenario.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether the full resolve-then-inspect pipeline should confirm
    ///     the resource as a downloadable image (<see langword="true" />) or not (<see langword="false" />).
    /// </summary>
    public bool ShouldDownload { get; set; }

    /// <summary>Gets or sets the URL under test. May be <see langword="null" /> or empty.</summary>
    public string? Url { get; set; }

    /// <inheritdoc />
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Provider), Provider);
        info.AddValue(nameof(Type), Type);
        info.AddValue(nameof(ShouldDownload), ShouldDownload);
        info.AddValue(nameof(Url), Url);
    }

    /// <inheritdoc />
    public void Deserialize(IXunitSerializationInfo info)
    {
        Provider = info.GetValue<string>(nameof(Provider)) ?? string.Empty;
        Type = info.GetValue<string>(nameof(Type)) ?? string.Empty;
        ShouldDownload = info.GetValue<bool>(nameof(ShouldDownload));
        Url = info.GetValue<string?>(nameof(Url));
    }
}
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Collector.Tests
 * File: TestCaseRow.cs
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
namespace SlideGenerator.Collector.Tests.Integration;

/// <summary>
///     Represents a single row from the integration test CSV file, mapping cloud provider metadata
///     and the expected download decision to a source URL.
/// </summary>
internal sealed record TestCaseRow
{
    /// <summary>Gets the cloud provider name (e.g., "Google Drive", "OneDrive", "Google Photos", "Raw").</summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Gets the human-readable description of the test scenario.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets whether the collector should decide to download this source.</summary>
    public bool ShouldDownload { get; init; }

    /// <summary>Gets the source URL for this test case, or <see langword="null" /> / empty if absent.</summary>
    public string? Url { get; init; }
}


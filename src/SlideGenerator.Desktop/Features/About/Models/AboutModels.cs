/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: AboutModels.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Desktop.Features.About.Models;

/// <summary>
///     One GitHub contributor to the repository (plan §5.7 "Developers") — sourced live from the GitHub REST
///     API, never fabricated. No role/badge field: the plan's crown/computer/paint role icons need an
///     official login→role map from the project owner (blueprint §8-Q2, not yet answered) — until then this
///     shows plain contributor identity only.
/// </summary>
public sealed record Contributor(string Login, string AvatarUrl, string ProfileUrl, int Contributions);

/// <summary>One GitHub Sponsor of the repository owner (plan §5.7 "Supporters") — sourced from the
///     <c>sponsors.json</c> a scheduled GitHub Action publishes (see <c>.github/workflows/sponsors.yml</c>),
///     never fabricated.</summary>
public sealed record Supporter(string Login, string AvatarUrl, string ProfileUrl);

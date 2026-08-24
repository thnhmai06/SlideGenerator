/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: RecentRunViewModel.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Generator.Jobs.Models;

namespace SlideGenerator.Desktop.Features.Recipes.ViewModels;

/// <summary>
///     One row in a recipe's "recent runs" list — immutable display data, not live-updated (unlike Runs'
///     own list). <c>Summary</c> itself carries no <c>RequestId</c> field (it lives only as the dictionary
///     key on <c>IService.ListActiveAsync</c>/<c>ListCompletedAsync</c>), so this record captures it alongside
///     the display fields for the "Xem tất cả" link into Runs.
/// </summary>
public sealed record RecentRunViewModel(string RequestId, string Name, JobStatus Status, DateTimeOffset CreatedAt);

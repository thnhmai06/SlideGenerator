/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Settings.Domain.Entities;

/// <summary>
///     Represents the root configuration entity containing all application settings.
/// </summary>
public sealed partial record Setting
{
    /// <summary>
    ///     Gets the configuration settings related to image downloading and resource fetching.
    /// </summary>
    public NetworkSetting Network { get; init; } = new();

    /// <summary>
    ///     Gets the configuration settings related to job execution and parallelism.
    /// </summary>
    public PerformanceSetting Performance { get; init; } = new();
}
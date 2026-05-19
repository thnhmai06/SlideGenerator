/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.cs
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
/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: ISettingProvider.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using SlideGenerator.Settings.Domain.Entities;

namespace SlideGenerator.Settings.Application.Abstractions;

/// <summary>
///     Provides read-only access to the current application configuration.
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    ///     Gets the current active <see cref="Setting" /> configuration.
    /// </summary>
    public Setting Current { get; }
}
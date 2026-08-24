/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Settings
 * File: Setting.AppearanceSetting.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Settings.Mutable;

public sealed partial record Setting
{
    public sealed record AppearanceSetting
    {
        /// <summary>Gets the theme variant to apply.</summary>
        public ThemeMode Theme { get; init; } = ThemeMode.System;

        /// <summary>
        ///     Gets the UI culture name (e.g. <c>"vi"</c>, <c>"en"</c>), or empty to follow
        ///     <see cref="System.Globalization.CultureInfo.CurrentUICulture" />.
        /// </summary>
        public string Language { get; init; } = string.Empty;

        /// <summary>Gets whether animations should be skipped in favor of instant state changes.</summary>
        public bool ReducedMotion { get; init; } = false;
    }
}

/// <summary>
///     Discriminates which theme variant the application should render.
/// </summary>
public enum ThemeMode
{
    /// <summary>Follow the operating system's theme preference.</summary>
    System,

    /// <summary>Always render the light variant.</summary>
    Light,

    /// <summary>Always render the dark variant.</summary>
    Dark
}

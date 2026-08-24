/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: ShellDestination.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

namespace SlideGenerator.Desktop.Shell;

/// <summary>
///     The three fixed sidebar destinations (see the plan's Information Architecture) — never more than
///     this, so this is a plain enum rather than a registry.
/// </summary>
public enum ShellDestination
{
    /// <summary>Recipe list and editor.</summary>
    Recipes,

    /// <summary>Active and completed generation requests.</summary>
    Runs,

    /// <summary>Appearance, performance, and network settings.</summary>
    Settings
}

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
///     The four fixed shell destinations (see the plan's Information Architecture) — never more than this,
///     so this is a plain enum rather than a registry. Recipes/Runs sit in the title toolbar's nav pill;
///     Settings/About are standalone icon buttons alongside it (blueprint §3.1).
/// </summary>
public enum ShellDestination
{
    /// <summary>Recipe list and editor.</summary>
    Recipes,

    /// <summary>Active and completed generation requests.</summary>
    Runs,

    /// <summary>Appearance, performance, and network settings.</summary>
    Settings,

    /// <summary>Product info, update check, developers, supporters (blueprint §5.7 — placeholder until P6).</summary>
    About
}

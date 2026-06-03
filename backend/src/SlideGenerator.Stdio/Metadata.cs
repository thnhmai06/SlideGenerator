/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: Metadata.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Reflection;
using SlideGenerator.Settings.Domain.Rules;

namespace SlideGenerator.Stdio;

// Don't ask why.
internal static class Metadata
{
    public const string Description =
        $"This is the {NameAndPaths.Application.Type} of {NameAndPaths.Application.Name}.";

    public const string Line = "────────────────────────────────────────────────────────────";

    public const string Repository =
        $"This software is FREE and OPEN-SOURCE. The source code is available here: {NameAndPaths.Application.Repository}";

    public static readonly string License =
        $"Copyright (c) {DateTime.Now.Year} {NameAndPaths.Application.Author}. Licensed under the {NameAndPaths.Application.License}.";

    public static readonly string Version =
        $"Version: v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
}
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

namespace SlideGenerator.Stdio;

// Don't ask why.
internal static class Metadata
{
    public const string Name =
        """
          /$$$$$$  /$$ /$$       /$$            /$$$$$$                                                     /$$                        
         /$$__  $$| $$|__/      | $$           /$$__  $$                                                   | $$                        
        | $$  \__/| $$ /$$  /$$$$$$$  /$$$$$$ | $$  \__/  /$$$$$$  /$$$$$$$   /$$$$$$   /$$$$$$  /$$$$$$  /$$$$$$    /$$$$$$   /$$$$$$ 
        |  $$$$$$ | $$| $$ /$$__  $$ /$$__  $$| $$ /$$$$ /$$__  $$| $$__  $$ /$$__  $$ /$$__  $$|____  $$|_  $$_/   /$$__  $$ /$$__  $$
         \____  $$| $$| $$| $$  | $$| $$$$$$$$| $$|_  $$| $$$$$$$$| $$  \ $$| $$$$$$$$| $$  \__/ /$$$$$$$  | $$    | $$  \ $$| $$  \__/
         /$$  \ $$| $$| $$| $$  | $$| $$_____/| $$  \ $$| $$_____/| $$  | $$| $$_____/| $$      /$$__  $$  | $$ /$$| $$  | $$| $$      
        |  $$$$$$/| $$| $$|  $$$$$$$|  $$$$$$$|  $$$$$$/|  $$$$$$$| $$  | $$|  $$$$$$$| $$     |  $$$$$$$  |  $$$$/|  $$$$$$/| $$      
         \______/ |__/|__/ \_______/ \_______/ \______/  \_______/|__/  |__/ \_______/|__/      \_______/   \___/   \______/ |__/      
        """;

    public const string Description = "This is the Backend sidecar of SlideGenerator.";

    public const string Line = "────────────────────────────────────────────────────────────";
    public const string License = "Copyright (c) 2026 Thành Mai (thnhmai06). Licensed under the GNU AGPLv3.";

    public const string RepositoryUrl =
        "This software is FREE and OPEN-SOURCE. The source code is available here: https://github.com/thnhmai06/SlideGenerator";

    public static readonly string Version =
        $"Version: v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
}
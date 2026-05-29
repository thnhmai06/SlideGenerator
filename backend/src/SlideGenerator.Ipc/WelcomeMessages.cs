/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Ipc
 * File: WelcomeMessages.cs
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

using System.Reflection;

namespace SlideGenerator.Ipc;

// Don't ask why.
internal static class WelcomeMessages
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
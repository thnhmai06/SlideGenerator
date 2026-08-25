/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: Metadata.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Reflection;
using SlideGenerator.Settings.Immutable;

namespace SlideGenerator.Desktop.Bootstrap;

// Don't ask why.
internal static class Metadata
{
    public static class Print
    {
        public const string Description = "An automated, template-based presentation generator.";

        public const string Repository =
            $"This software is FREE and OPEN-SOURCE. The source code is available here: {Value.Repository}";

        public static readonly string License =
            $"Copyright (c) {DateTime.Now.Year} {Value.Author}. Licensed under the {Value.License}.";

        /// <summary>The ASCII art representation of the application name.</summary>
        public const string NameArt =
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

        public const string Portable = NameAndPaths.Portable ? "Portable" : "Installer";
    }

    public static class Value
    {
        /// <summary>The official application repository URL.</summary>
        public const string Repository = "https://github.com/thnhmai06/SlideGenerator";

        /// <summary>The official application author.</summary>
        public const string Author = "Thành Mai (thnhmai06)";

        /// <summary>The license under which the application is distributed.</summary>
        public const string License = "Apache-2.0";

        /// <summary>The running assembly's informational version (falls back to the assembly version, then
        ///     <c>"unknown"</c>) — same lookup <c>Program.PrintMetadata</c> uses for the startup log line.</summary>
        public static string Version
        {
            get
            {
                var assembly = typeof(Metadata).Assembly;
                return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?? assembly.GetName().Version?.ToString()
                       ?? "unknown";
            }
        }
    }
}
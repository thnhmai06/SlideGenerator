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

    public static readonly string Version =
        $"Version: v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";

    public const string Description = "This is the Backend Sidecar of SlideGenerator.";

    public const string Line = "────────────────────────────────────────────────────────────";
    public const string License = "Copyright (c) 2026 Mai Thành. Licensed under the GNU AGPLv3.";
    public const string RepositoryUrl = "You can find app source code at: https://github.com/thnhmai06/SlideGenerator";
}
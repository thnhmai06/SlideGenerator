/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Desktop
 * File: Program.cs
 *
 * This file is part of this solution.
 * You can find the full source code here: https://github.com/thnhmai06/SlideGenerator.
 *
 * Licensed under the Apache License 2.0.
 * See the LICENSE file in the project root for full license information.
 */

using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SlideGenerator.Desktop.Bootstrap;
using SlideGenerator.Logging.Formats;
using SlideGenerator.Settings.Database;
using SlideGenerator.Settings.Immutable;
using Velopack;

namespace SlideGenerator.Desktop;

/// <summary>
///     Application entry point for the Avalonia desktop client.
///     Bootstraps single-instance guard, system logging, DB migration, and Velopack before
///     handing control to the Avalonia classic desktop lifetime.
/// </summary>
internal static class Program
{
    public static readonly DateTime StartupTime = DateTime.UtcNow;
    private static string? _logFilePath;

    private static readonly Lazy<SingleInstanceLock> InstanceLock = new(() =>
        new SingleInstanceLock(NameAndPaths.AppLocker.MutexName, NameAndPaths.AppLocker.PidPath));

    /// <summary>Application entry point.</summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        // Must run before anything else — handles Velopack's install/uninstall/update hooks and may exit the
        // process immediately without returning.
        VelopackApp.Build().Run();

        ConfigureEncoding();

        var bootstrapConfiguration = new ConfigurationBuilder()
            .SetBasePath(NameAndPaths.BasePath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        EnsureSingleInstance();
        BootstrapSystemLogger(bootstrapConfiguration);

        Directory.CreateDirectory(NameAndPaths.DataFolder.FolderPath);
        DatabaseMigrator.Migrate(NameAndPaths.DataFolder.DataFile.ConnectionString);

        PrintMetadata();
        RegisterExceptionHandlers();

        try
        {
            Log.Information("Application starting... (PID: {ProcessId})", Environment.ProcessId);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal exception in Main");
#if DEBUG
            throw;
#endif
        }
        finally
        {
            if (InstanceLock.IsValueCreated) InstanceLock.Value.Dispose();
            Log.Information("Goodbye!");
            Log.CloseAndFlush();
        }
    }

    /// <summary>Configures the Avalonia application builder.</summary>
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    /// <summary>
    ///     Ensures only one instance of the application runs at a time.
    ///     Exits immediately without creating a log file if another instance is detected.
    /// </summary>
    private static void EnsureSingleInstance()
    {
        if (InstanceLock.Value.TryAcquire()) return;

        var pid = InstanceLock.Value.ReadPid();
        Console.Error.WriteLine($"{NameAndPaths.Application.Name} is already running with PID: {pid}. Exiting.");
        Environment.Exit(1);
    }

    /// <summary>Configures stderr to use UTF-8 so Serilog console output is transmitted correctly.</summary>
    private static void ConfigureEncoding()
    {
        Console.SetError(new StreamWriter(
            Console.OpenStandardError(),
            new UTF8Encoding(false),
            leaveOpen: true) { AutoFlush = true });
    }

    /// <summary>
    ///     Configures the global Serilog logger (file + stderr sinks) for pre-DI logging.
    /// </summary>
    private static void BootstrapSystemLogger(IConfiguration configuration)
    {
        var systemLogDirectory = NameAndPaths.LogsFolder.SystemPath;
        Directory.CreateDirectory(systemLogDirectory);
        _logFilePath = Path.Combine(systemLogDirectory, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        var level = configuration.GetValue("Logging:System:MinimumLevel", LogEventLevel.Debug);
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("LoggerName", "System")
            .Enrich.WithProperty("Scope", "Global")
            .WriteTo.File(new FileLogFormatter([]), _logFilePath)
            .WriteTo.Console(new ConsoleLogFormatter(), standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        Log.Logger = serilogLogger;
    }

    /// <summary>Prints the ASCII art banner and build metadata to the system log.</summary>
    private static void PrintMetadata()
    {
        var assembly = typeof(Program).Assembly;
        var version =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        var gcInfo = GC.GetGCMemoryInfo();
        var appDrive = new DriveInfo(Path.GetPathRoot(AppContext.BaseDirectory)!);

        PrintLine();
        PrintArt(Metadata.Print.NameArt);

        Log.Information($"{NameAndPaths.Application.Name} v{version} ({Metadata.Print.Portable})");
        Log.Information(Metadata.Print.Description);

        PrintLine();

        Log.Information(Metadata.Print.License);
        Log.Information(Metadata.Print.Repository);

        PrintLine();

        Log.Information("System Information");
        PrintKeyValue("Machine", Environment.MachineName);
        PrintKeyValue("OS", RuntimeInformation.OSDescription);
        PrintKeyValue("OS Version", Environment.OSVersion.Version);
        PrintKeyValue("OS Architecture", RuntimeInformation.OSArchitecture);
        PrintKeyValue("Runtime", RuntimeInformation.FrameworkDescription);
        PrintKeyValue("Runtime Identifier", RuntimeInformation.RuntimeIdentifier);
        PrintKeyValue("CLR", Environment.Version);
        PrintKeyValue("Process Architecture", RuntimeInformation.ProcessArchitecture);
        PrintKeyValue("Process ID", Environment.ProcessId);
        PrintKeyValue("Process Name",
            Environment.ProcessPath is { } path
                ? Path.GetFileNameWithoutExtension(path)
                : "Unknown");
        PrintKeyValue("Logical CPUs", Environment.ProcessorCount);
        PrintKeyValue("64-bit OS", Environment.Is64BitOperatingSystem);
        PrintKeyValue("64-bit Process", Environment.Is64BitProcess);

        Log.Information("GC");
        PrintKeyValue("Server GC", GCSettings.IsServerGC);
        PrintKeyValue("Latency Mode", GCSettings.LatencyMode);
        PrintKeyValue(
            "Memory Limit",
            $"{gcInfo.TotalAvailableMemoryBytes / 1024d / 1024 / 1024:F2} GiB");
        PrintKeyValue(
            "Heap Size",
            $"{gcInfo.HeapSizeBytes / 1024d / 1024:F2} MiB");

        Log.Information("Localization");
        PrintKeyValue("Time Zone", TimeZoneInfo.Local.DisplayName);
        PrintKeyValue("Time Zone ID", TimeZoneInfo.Local.Id);
        PrintKeyValue("Culture", CultureInfo.CurrentCulture.Name);
        PrintKeyValue("UI Culture", CultureInfo.CurrentUICulture.Name);

        Log.Information("Storage");
        PrintKeyValue("Application Drive", appDrive.Name);
        PrintKeyValue("Drive Format", appDrive.DriveFormat);
        PrintKeyValue(
            "Total Space",
            $"{appDrive.TotalSize / 1024d / 1024 / 1024:F2} GiB");
        PrintKeyValue(
            "Free Space",
            $"{appDrive.AvailableFreeSpace / 1024d / 1024 / 1024:F2} GiB");

        Log.Information("Paths");
        PrintKeyValue("Working Directory", Environment.CurrentDirectory);
        PrintKeyValue("Base Directory", AppContext.BaseDirectory);
        PrintKeyValue("Command Line", Environment.CommandLine);

        PrintLine();
        return;

        static void PrintKeyValue(string key, object? value)
        {
            Log.Information($"\t{key,-22}: {value}");
        }

        static void PrintLine()
        {
            Log.Information("────────────────────────────────────────────────────────");
        }
    }

    private static void PrintArt(string art)
    {
        foreach (var line in art.Split('\n')) Log.Information(line.TrimEnd('\r'));
    }

    /// <summary>Registers process-wide unhandled exception and task exception handlers.</summary>
    private static void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}",
                    e.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Fatal(e.Exception, "Unobserved Task exception.");
#if !DEBUG
            e.SetObserved();
#endif
        };
    }
}
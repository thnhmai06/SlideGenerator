/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Stdio
 * File: Program.cs
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using SlideGenerator.Logging.Formats;
using SlideGenerator.Settings.Domain.Rules;
using SlideGenerator.Stdio.Implementations;

namespace SlideGenerator.Stdio;

/// <summary>
///     Application entry point for the JSON-RPC 2.0 IPC sidecar.
///     Bootstraps the host, wires all services and method routes via StreamJsonRpc,
///     then blocks until the client closes the connection.
/// </summary>
internal static partial class Program
{
    public static readonly DateTime StartupTime = DateTime.UtcNow;
    private static string? _logFilePath;

    private static readonly Lazy<SingleInstanceLock> InstanceLock = new(() =>
        new SingleInstanceLock(NameAndPaths.AppLocker.MutexName, NameAndPaths.AppLocker.PidPath));

    /// <summary>Application entry point.</summary>
    /// <param name="args">Command-line arguments passed by the Tauri sidecar launcher.</param>
    public static async Task<int> Main(string[] args)
    {
        ConfigureEncoding();

        var bootstrapConfiguration = new ConfigurationBuilder()
            .SetBasePath(NameAndPaths.BasePath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        EnsureSingleInstance();
        BootstrapSystemLogger(bootstrapConfiguration);
        PrintWelcomeMessage();

        try
        {
            Log.Information("Application starting... (PID: {ProcessId})", Environment.ProcessId);
            RegisterExceptionHandlers();

            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services);
            using var host = builder.Build();

            RegisterCtrlCHandler(host);
            await host.StartAsync().ConfigureAwait(false);

            await StartupAsync(host, JsonRpcBootstrap.BuildJsonSerializerOptions()).ConfigureAwait(false);
            await host.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal exception in Main");
#if DEBUG
            throw;
#else
            return 1;
#endif
        }
        finally
        {
            if (InstanceLock.IsValueCreated) InstanceLock.Value.Dispose();
            Log.Information("Goodbye!");
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }

        return 0;
    }

    /// <summary>
    ///     Ensures only one instance of the application runs at a time.
    ///     A named <see cref="Mutex" /> is the authoritative lock; a PID file records the owner's process ID
    ///     so a second instance can display it before exiting.
    ///     Exits immediately without creating a log file if another instance is detected.
    /// </summary>
    private static void EnsureSingleInstance()
    {
        if (InstanceLock.Value.TryAcquire()) return;

        var pid = InstanceLock.Value.ReadPid();
        Console.Error.WriteLine($"{NameAndPaths.AppName} is already running with PID: {pid}. Exiting.");
        Environment.Exit(1);
    }

    /// <summary>
    ///     Configures the global Serilog logger (file + stderr sinks) for pre-DI logging.
    ///     Must be called before <see cref="ConfigureServices" />.
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
            .WriteTo.File(new FileLogFormatter(), _logFilePath)
            .WriteTo.Console(new ConsoleLogFormatter(), standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();

        Log.Logger = serilogLogger;
    }
}

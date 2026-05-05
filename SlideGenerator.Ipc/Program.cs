using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SlideGenerator.Cloud;
using SlideGenerator.Coordinator;
using SlideGenerator.Document;
using SlideGenerator.Download;
using SlideGenerator.Image;
using SlideGenerator.Ipc.Handlers;
using SlideGenerator.Ipc.Ipc;
using SlideGenerator.Ipc.Ipc.Adapters;
using SlideGenerator.Logging;
using SlideGenerator.Pipeline.Generating;
using SlideGenerator.Pipeline.Generating.Workflows;
using SlideGenerator.Pipeline.Generating.Workflows.Models;
using SlideGenerator.Pipeline.Scanning;
using SlideGenerator.Settings;
using SlideGenerator.Settings.Services;
using StreamJsonRpc;
using WorkflowCore.Interface;

namespace SlideGenerator.Ipc;

/// <summary>
///     Application entry point for the JSON-RPC 2.0 IPC sidecar.
///     Bootstraps the host, wires all services and method routes via StreamJsonRpc,
///     then blocks until the client closes the connection.
/// </summary>
internal static class Program
{
    public static readonly DateTime StartupTime = DateTime.UtcNow;
    private static string? _logFilePath;
    private static IHost? _currentHost;

    // ── Native Interop for Shutdown Handling ─────────────────────────────────────

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);

    /// <summary>Application entry point.</summary>
    /// <param name="args">Command-line arguments passed by the Tauri sidecar launcher.</param>
    public static async Task<int> Main(string[] args)
    {
        ConfigureEncoding();

        // 1. Setup bootstrap configuration to initialize logging as early as possible
        var bootstrapConfig = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // 2. Generate log file path and initialize the logger module first
        var logFileName = $"{DateTime.Now:yyyy-MM-dd HH-mm-ss}.log";
        _logFilePath = Path.Combine(LoggingPaths.LogFolderPath, logFileName);

        Logging.Registration.ConfigureStaticLogger(bootstrapConfig, _logFilePath);

        PrintWelcomeMessage();

        // Wire up native console control handler for 'X' button, Logoff, and Shutdown
        SetConsoleCtrlHandler(ctrlType =>
        {
            Log.Warning("Native termination signal received: {CtrlType}. Flushing and exiting...", ctrlType);

            // Note: We have very limited time here before OS kills the process.
            // We attempt a sync save and flush.
            if (_currentHost != null) SaveSettingsAsync(_currentHost.Services).GetAwaiter().GetResult();
            Log.CloseAndFlush();
            return false; // Let the next handler (or OS) deal with it
        }, true);

        try
        {
            Log.Information("Application starting...");

            // Set up global exception logging and prevent crashes where possible
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    Log.Fatal(ex, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}", e.IsTerminating);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log.Fatal(e.Exception, "Unobserved Task exception.");
#if !DEBUG
                e.SetObserved();
#endif
            };

            // Ensure logs are flushed on unexpected process exit
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                Log.Information("Process exiting, flushing logs...");
                Log.CloseAndFlush();
            };

            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services, builder.Configuration);

            using var host = builder.Build();
            _currentHost = host;

            // Intercept Ctrl+C to trigger a clean shutdown via the host lifetime
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // Prevent immediate termination
                Log.Warning("Ctrl+C detected. Initiating graceful shutdown...");
                lifetime.StopApplication();
            };

            await host.StartAsync();

            var jsonOptions = BuildJsonSerializerOptions();
            await StartupAsync(host, jsonOptions);

            await host.StopAsync();
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
            await Log.CloseAndFlushAsync();
        }

        return 0;
    }

    // ── Encoding ─────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Configures stderr to use UTF-8 so Serilog log output is transmitted correctly
    ///     regardless of the platform's default code page.
    ///     stdin and stdout are owned by StreamJsonRpc which writes raw UTF-8 bytes directly,
    ///     bypassing <see cref="Console" /> encoding wrappers.
    /// </summary>
    private static void ConfigureEncoding()
    {
        Console.SetError(new StreamWriter(
            Console.OpenStandardError(),
            new UTF8Encoding(false),
            leaveOpen: true) { AutoFlush = true });
    }

    // ── DI Registration ───────────────────────────────────────────────────────────

    /// <summary>
    ///     Registers all services into the DI container. Delegates to per-module
    ///     <c>Registration.cs</c> extension methods following the project convention.
    ///     The <see cref="JsonRpc" /> connection is not registered in the container
    ///     because it requires stream access available only after host construction.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">The application configuration, required by the logging module.</param>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Registering core services...");
        services.AddSystemLogging(configuration, _logFilePath ?? string.Empty);
        services.AddWorkflowLogging();

        Log.Information("Registering domain modules...");
        services.AddSettingServices();
        services.AddDocumentServices();
        services.AddCoreServices();
        services.AddCloudServices();
        services.AddDownloadServices();
        services.AddImageServices();
        services.AddGeneratingServices();
        services.AddTransient<ScanningService>();

        Log.Information("Registering workflow and IPC infrastructure...");
        services.AddWorkflow();
        services.AddIpcServices();
    }

    // ── Startup & Run ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     Performs the ordered startup sequence, then blocks on <see cref="JsonRpc.Completion" />
    ///     until the client closes the connection. Tears down all resources in reverse order on exit.
    /// </summary>
    /// <param name="host">The fully built application host.</param>
    /// <param name="jsonOptions">Serializer options forwarded to the StreamJsonRpc formatter.</param>
    private static async Task StartupAsync(IHost host, JsonSerializerOptions jsonOptions)
    {
        var services = host.Services;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SlideGenerator.Ipc.Program");
        IWorkflowHost? workflowHost = null;
        JsonRpc? jsonRpc = null;

        try
        {
            Log.Information("Loading settings...");
            await LoadSettingsAsync(services);

            Log.Information("Starting workflow host...");
            workflowHost = await StartWorkflowHostAsync(services);

            Log.Information("Initializing JSON-RPC connection...");
            jsonRpc = CreateAndConfigureJsonRpc(services, jsonOptions);
            AttachProgressObserver(services, jsonRpc);

            Log.Information("Setup completed! Application is listening.");

            // Wait for either the RPC connection to close OR the host to signal shutdown (e.g. via Ctrl+C)
            var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
            await Task.WhenAny(jsonRpc.Completion, Task.Delay(-1, lifetime.ApplicationStopping));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred during IPC lifecycle.");
#if DEBUG
            throw;
#endif
        }
        finally
        {
            await TeardownAsync(host, services, workflowHost, jsonRpc);
        }
    }

    /// <summary>
    ///     Prints the application welcome message and metadata from <see cref="WelcomeMessages" /> using the logger.
    /// </summary>
    private static void PrintWelcomeMessage()
    {
        Log.Information("\n{AsciiArt}", WelcomeMessages.Name);
        Log.Information(WelcomeMessages.Line);
        Log.Information(WelcomeMessages.Version);
        Log.Information(WelcomeMessages.Description);
        Log.Information(WelcomeMessages.Line);
        Log.Information(WelcomeMessages.License);
        Log.Information(WelcomeMessages.RepositoryUrl);
        Log.Information(WelcomeMessages.Line);
    }

    /// <summary>Registers the generating workflow and starts the WorkflowCore host.</summary>
    /// <param name="services">The application service provider.</param>
    /// <returns>The running <see cref="IWorkflowHost" /> for later shutdown.</returns>
    private static async Task<IWorkflowHost> StartWorkflowHostAsync(IServiceProvider services)
    {
        var workflowHost = services.GetRequiredService<IWorkflowHost>();
        workflowHost.RegisterWorkflow<GeneratingWorkflow, GeneratingTask>();
        await workflowHost.StartAsync(CancellationToken.None);
        return workflowHost;
    }

    /// <summary>
    ///     Creates the <see cref="JsonRpc" /> connection over stdin/stdout using NDJSON framing
    ///     and STJ serialization, then registers all method handlers by their JSON-RPC method names.
    ///     Calls <see cref="JsonRpc.StartListening" /> before returning.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="jsonOptions">STJ options shared with the <see cref="SystemTextJsonFormatter" />.</param>
    /// <returns>The started <see cref="JsonRpc" /> instance.</returns>
    private static JsonRpc CreateAndConfigureJsonRpc(IServiceProvider services, JsonSerializerOptions jsonOptions)
    {
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = jsonOptions };
        var handler = new NewLineDelimitedMessageHandler(
            Console.OpenStandardOutput(),
            Console.OpenStandardInput(),
            formatter);
        var jsonRpc = new JsonRpc(handler)
        {
            CancelLocallyInvokedMethodsWhenConnectionIsClosed = true
        };

        var workflowHandler = services.GetRequiredService<WorkflowHandler>();
        var scanningHandler = services.GetRequiredService<ScanningHandler>();
        var settingsHandler = services.GetRequiredService<SettingsHandler>();

        // Workflow lifecycle
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.StartAsync)), workflowHandler,
            Attr("workflow.start"));
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.CancelAsync)), workflowHandler,
            Attr("workflow.cancel"));
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.PauseAsync)), workflowHandler,
            Attr("workflow.pause"));
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.ResumeAsync)), workflowHandler,
            Attr("workflow.resume"));

        // Scanning
        jsonRpc.AddLocalRpcMethod(GetMethod<ScanningHandler>(nameof(ScanningHandler.ScanWorkbookAsync)),
            scanningHandler, Attr("scanning.scanWorkbook"));
        jsonRpc.AddLocalRpcMethod(GetMethod<ScanningHandler>(nameof(ScanningHandler.ScanPresentationAsync)),
            scanningHandler, Attr("scanning.scanPresentation"));

        // Settings — Get and ResetToDefaults carry no parameters, so no UseSingleObjectParameterDeserialization.
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.GetAsync)), settingsHandler,
            new JsonRpcMethodAttribute("settings.get"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.UpdateAsync)), settingsHandler,
            Attr("settings.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.ResetToDefaultsAsync)),
            settingsHandler, new JsonRpcMethodAttribute("settings.resetToDefaults"));

        jsonRpc.StartListening();
        return jsonRpc;

        // Helpers — named to avoid lambda capture overhead in the hot path.
        static JsonRpcMethodAttribute Attr(string name)
        {
            return new JsonRpcMethodAttribute(name) { UseSingleObjectParameterDeserialization = true };
        }

        static MethodInfo GetMethod<T>(string name)
        {
            return typeof(T).GetMethod(name) ??
                   throw new InvalidOperationException($"Method {name} not found on {typeof(T).Name}");
        }
    }

    /// <summary>
    ///     Attaches the <see cref="WorkflowProgressObserver" /> to the <see cref="WorkflowEventBus" />
    ///     so workflow lifecycle events are forwarded as <c>workflow/progress</c> notifications on stdout.
    /// </summary>
    /// <param name="services">The application service provider.</param>
    /// <param name="jsonRpc">The active JSON-RPC connection used to send notifications.</param>
    private static void AttachProgressObserver(IServiceProvider services, JsonRpc jsonRpc)
    {
        var eventBus = services.GetRequiredService<WorkflowEventBus>();
        var observer = services.GetRequiredService<WorkflowProgressObserver>();
        observer.Attach(eventBus, jsonRpc);
    }

    /// <summary>Shuts down all components in the reverse of startup order.</summary>
    /// <param name="host">The application host.</param>
    /// <param name="services">The application service provider.</param>
    /// <param name="workflowHost">The WorkflowCore host to stop.</param>
    /// <param name="jsonRpc">The JSON-RPC connection to dispose.</param>
    private static async Task TeardownAsync(
        IHost host,
        IServiceProvider services,
        IWorkflowHost? workflowHost,
        JsonRpc? jsonRpc)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SlideGenerator.Ipc.Program");

        try
        {
            var eventBus = services.GetRequiredService<WorkflowEventBus>();
            services.GetRequiredService<WorkflowProgressObserver>().Detach(eventBus);

            jsonRpc?.Dispose();

            if (workflowHost != null) await workflowHost.StopAsync(CancellationToken.None);

            await SaveSettingsAsync(services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during TeardownAsync.");
        }
        finally
        {
            await host.StopAsync();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>Loads persisted settings from disk before any workflow is started.</summary>
    /// <param name="services">The application service provider.</param>
    private static async Task LoadSettingsAsync(IServiceProvider services)
    {
        var settingManager = services.GetRequiredService<SettingManager>();
        await settingManager.Load();
    }

    /// <summary>Persists current settings to disk before application shutdown.</summary>
    /// <param name="services">The application service provider.</param>
    private static async Task SaveSettingsAsync(IServiceProvider services)
    {
        var settingManager = services.GetRequiredService<SettingManager>();
        await settingManager.Save();
    }

    /// <summary>
    ///     Builds the shared <see cref="JsonSerializerOptions" /> passed to the
    ///     <see cref="SystemTextJsonFormatter" /> for all JSON-RPC serialization.
    /// </summary>
    private static JsonSerializerOptions BuildJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new RoiOptionJsonAdapter(),
                new RectangleFJsonAdapter(),
                new JsonStringEnumConverter()
            }
        };
    }

    private delegate bool ConsoleCtrlDelegate(CtrlTypes ctrlType);

    private enum CtrlTypes
    {
        // ReSharper disable once InconsistentNaming
        CTRL_C_EVENT = 0,

        // ReSharper disable once InconsistentNaming
        CTRL_BREAK_EVENT = 1,

        // ReSharper disable once InconsistentNaming
        CTRL_CLOSE_EVENT = 2,

        // ReSharper disable once InconsistentNaming
        CTRL_LOGOFF_EVENT = 5,

        // ReSharper disable once InconsistentNaming
        CTRL_SHUTDOWN_EVENT = 6
    }
}
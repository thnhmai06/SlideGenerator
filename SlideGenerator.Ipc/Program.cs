using System.Reflection;
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
using SlideGenerator.Documents;
using SlideGenerator.Download;
using SlideGenerator.Images;
using SlideGenerator.Ipc.Handlers;
using SlideGenerator.Ipc.Ipc;
using SlideGenerator.Ipc.Ipc.Adapters;
using SlideGenerator.Logging;
using SlideGenerator.Pipelines.Generating;
using SlideGenerator.Pipelines.Generating.Workflows;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using SlideGenerator.Pipelines.Scanning;
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
    /// <summary>Application entry point.</summary>
    /// <param name="args">Command-line arguments passed by the Tauri sidecar launcher.</param>
    public static async Task<int> Main(string[] args)
    {
        ConfigureEncoding();

        // 1. Setup bootstrap configuration to initialize logging as early as possible
        var bootstrapConfig = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // 2. Initialize the logger module first
        SlideGenerator.Logging.Registration.ConfigureStaticLogger(bootstrapConfig);

        try
        {
            Log.Information("Application starting...");

            // Set up global exception logging and prevent crashes where possible
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Unhandled AppDomain exception. IsTerminating: {IsTerminating}", e.IsTerminating);
                }
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log.Fatal(e.Exception, "Unobserved Task exception.");
#if !DEBUG
                e.SetObserved();
#endif
            };

            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services, builder.Configuration);

            var host = builder.Build();
            var jsonOptions = BuildJsonSerializerOptions();
            await StartupAsync(host, jsonOptions);

            return 0;
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
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
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
        // Logging — stderr-only sinks; stdout is exclusively owned by the StreamJsonRpc layer.
        services.AddSystemLogging(configuration);
        services.AddWorkflowLogging();

        // Domain modules — order follows the dependency graph (foundation → core → feature).
        services.AddSettingServices();
        services.AddDocumentServices();
        services.AddCoreServices();
        services.AddCloudServices();
        services.AddDownloadServices();
        services.AddImageServices();
        services.AddGeneratingServices();
        services.AddTransient<ScanningService>();

        // WorkflowCore — defaults to in-memory persistence and single-node concurrency.
        services.AddWorkflow();

        // IPC handlers and supporting infrastructure.
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
            await LoadSettingsAsync(services);
            workflowHost = await StartWorkflowHostAsync(services);
            jsonRpc = CreateAndConfigureJsonRpc(services, jsonOptions);
            AttachProgressObserver(services, jsonRpc);

            await jsonRpc.Completion;
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
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.StartAsync)), workflowHandler, Attr("workflow.start"));
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.CancelAsync)), workflowHandler, Attr("workflow.cancel"));
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.PauseAsync)), workflowHandler, Attr("workflow.pause"));
        jsonRpc.AddLocalRpcMethod(GetMethod<WorkflowHandler>(nameof(WorkflowHandler.ResumeAsync)), workflowHandler, Attr("workflow.resume"));

        // Scanning
        jsonRpc.AddLocalRpcMethod(GetMethod<ScanningHandler>(nameof(ScanningHandler.ScanWorkbookAsync)), scanningHandler, Attr("scanning.scanWorkbook"));
        jsonRpc.AddLocalRpcMethod(GetMethod<ScanningHandler>(nameof(ScanningHandler.ScanPresentationAsync)), scanningHandler, Attr("scanning.scanPresentation"));

        // Settings — Get and ResetToDefaults carry no parameters, so no UseSingleObjectParameterDeserialization.
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.GetAsync)), settingsHandler, new JsonRpcMethodAttribute("settings.get"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.UpdateAsync)), settingsHandler, Attr("settings.update"));
        jsonRpc.AddLocalRpcMethod(GetMethod<SettingsHandler>(nameof(SettingsHandler.ResetToDefaultsAsync)), settingsHandler, new JsonRpcMethodAttribute("settings.resetToDefaults"));

        jsonRpc.StartListening();
        return jsonRpc;

        // Helpers — named to avoid lambda capture overhead in the hot path.
        static JsonRpcMethodAttribute Attr(string name) =>
            new(name) { UseSingleObjectParameterDeserialization = true };

        static MethodInfo GetMethod<T>(string name) =>
            typeof(T).GetMethod(name) ?? throw new InvalidOperationException($"Method {name} not found on {typeof(T).Name}");
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

            if (workflowHost != null)
            {
                await workflowHost.StopAsync(CancellationToken.None);
            }

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
}

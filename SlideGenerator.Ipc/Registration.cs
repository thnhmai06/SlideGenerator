using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Ipc.Handlers;
using SlideGenerator.Ipc.Ipc;

namespace SlideGenerator.Ipc;

/// <summary>
///     Provides extension methods to register all IPC infrastructure and handler services
///     into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds all IPC services: the workflow event bus, progress observer,
    ///     and all JSON-RPC method handlers.
    ///     The <see cref="StreamJsonRpc.JsonRpc" /> connection is created in <c>Program.cs</c>
    ///     after the host is built, and is not registered in the container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddIpcServices(this IServiceCollection services)
    {
        // Workflow progress event bus (decoupled from WorkflowCore internals)
        services.AddSingleton<WorkflowEventBus>();
        services.AddSingleton<WorkflowProgressObserver>();

        // JSON-RPC method handlers
        services.AddSingleton<WorkflowHandler>();
        services.AddSingleton<ScanningHandler>();
        services.AddSingleton<SettingsHandler>();

        return services;
    }
}

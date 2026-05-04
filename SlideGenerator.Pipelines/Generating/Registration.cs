using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Pipelines.Generating.Steps;
using SlideGenerator.Pipelines.Generating.Workflows;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using WorkflowCore.Interface;

namespace SlideGenerator.Pipelines.Generating;

/// <summary>
///     Provides extension methods to register the generating workflow and its steps
///     into the dependency injection container.
/// </summary>
public static class Registration
{
    /// <summary>
    ///     Adds the generating workflow and all associated steps to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddGeneratingServices(this IServiceCollection services)
    {
        // Step Registrations
        services.AddTransient<ValidateRequest>();
        services.AddTransient<CreateTemplate>();
        services.AddTransient<ExtractData>();
        services.AddTransient<DownloadImage>();
        services.AddTransient<EditImage>();
        services.AddTransient<ReplaceSlideData>();
        services.AddTransient<CloseAllHandles>();

        // Workflow Registration
        // Note: WorkflowCore expects the workflow to be registered via IWorkflowHost,
        // but often we just register the IWorkflow implementation in DI.
        services.AddTransient<IWorkflow<GeneratingTask>, GeneratingWorkflow>();

        return services;
    }
}

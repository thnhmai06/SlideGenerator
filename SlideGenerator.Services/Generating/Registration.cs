using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Services.Generating.Steps;
using SlideGenerator.Services.Generating.Workflows;
using WorkflowCore.Interface;

namespace SlideGenerator.Services.Generating;

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
        services.AddTransient<PrepareIterationTasks>();
        services.AddTransient<ExtractShapeBounds>();
        services.AddTransient<ExtractRowData>();
        services.AddTransient<DownloadImage>();
        services.AddTransient<EditImage>();
        services.AddTransient<ReplaceShapeData>();
        services.AddTransient<FinalizePresentation>();

        // Workflow Registration
        // Note: WorkflowCore expects the workflow to be registered via IWorkflowHost
        // but often we just register the IWorkflow implementation in DI.
        services.AddTransient<IWorkflow<GeneratingData>, GeneratingWorkflow>();

        return services;
    }
}

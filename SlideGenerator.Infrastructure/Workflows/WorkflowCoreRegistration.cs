using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Application.Modules.Lock.Steps;
using SlideGenerator.Application.Services.Generating;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Generating.Workflows.Activities;
using SlideGenerator.Application.Services.Scanning;
using SlideGenerator.Application.Services.Scanning.Workflows;
using SlideGenerator.Application.Services.Scanning.Workflows.Activities;
using WorkflowCore.Interface;

namespace SlideGenerator.Infrastructure.Workflows;

/// <summary>
///     Provides extension methods for registering WorkflowCore-related services and workflows.
/// </summary>
public static class WorkflowCoreRegistration
{
    /// <summary>
    ///     Adds WorkflowCore and its associated steps, activities, workflows, and services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The updated <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddWorkflowCoreInfrastructure(this IServiceCollection services)
    {
        services.AddWorkflow();
        
        // Steps
        services.AddTransient<AcquireSlotStep>();
        services.AddTransient<ReleaseSlotStep>();
        
        // Scanning Activities
        services.AddTransient<ScanWorkbook>();
        services.AddTransient<ScanPresentation>();
        
        // Generating Activities
        services.AddTransient<SimplyInstructions>();
        services.AddTransient<CreateWorkingPresentation>();
        services.AddTransient<DownloadImage>();
        services.AddTransient<EditImage>();
        services.AddTransient<CloneTemplateSlide>();
        services.AddTransient<EditSlide>();
        services.AddTransient<RemoveWorkingTemplateSlide>();

        // Workflows
        services.AddTransient<ScanningWorkflow>();
        services.AddTransient<GeneratingWorkflow>();

        // Services
        services.AddTransient<IScanningService, ScanningService>();
        services.AddTransient<IGeneratingService, GeneratingService>();

        return services;
    }

    /// <summary>
    ///     Registers the workflows and starts the workflow host.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider" /> used to resolve the workflow host.</param>
    public static void RegisterWorkflows(this IServiceProvider serviceProvider)
    {
        var host = serviceProvider.GetRequiredService<IWorkflowHost>();
        host.RegisterWorkflow<ScanningWorkflow, ScanningData>();
        host.RegisterWorkflow<GeneratingWorkflow, GeneratingData>();
        host.Start();
    }
}

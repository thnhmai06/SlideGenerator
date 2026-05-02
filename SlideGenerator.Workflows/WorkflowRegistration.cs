using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Workflows.Generating;
using SlideGenerator.Workflows.Generating.Activities;
using SlideGenerator.Workflows.Scanning;
using SlideGenerator.Workflows.Scanning.Activities;
using SlideGenerator.Workflows.Services;
using WorkflowCore.Interface;

namespace SlideGenerator.Workflows;

public static class WorkflowRegistration
{
    public static IServiceCollection AddWorkflowInfrastructure(this IServiceCollection services)
    {
        services.AddWorkflow();
        
        // Activities
        services.AddTransient<AcquireSlotStep>();
        services.AddTransient<ReleaseSlotStep>();
        services.AddTransient<ScanWorkbook>();
        services.AddTransient<ScanPresentation>();
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
        services.AddTransient<ScanningService>();
        services.AddTransient<GeneratingService>();

        return services;
    }

    public static void RegisterWorkflows(this IServiceProvider serviceProvider)
    {
        var host = serviceProvider.GetRequiredService<IWorkflowHost>();
        host.RegisterWorkflow<ScanningWorkflow, ScanningData>();
        host.RegisterWorkflow<GeneratingWorkflow, GeneratingData>();
        host.Start();
    }
}
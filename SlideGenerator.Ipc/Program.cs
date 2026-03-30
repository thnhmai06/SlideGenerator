using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Framework.Features.Image.Contracts;
using SlideGenerator.Framework.Features.Image.Services;
using SlideGenerator.Ipc.Endpoints;
using SlideGenerator.Application;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Generating.Services;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Domain.Configs.Contracts;
using SlideGenerator.Domain.Configs.Entities;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Settings.Services;
using SlideGenerator.Infrastructure.Sheet.Adapter;
using SlideGenerator.Infrastructure.Slide.Adapters;
using SlideGenerator.Infrastructure.Slide.Services;
using StreamJsonRpc;

namespace SlideGenerator.Ipc;

public static class Program
{
    public static async Task Main()
    {
        var services = ConfigureServices();

        await using var serviceProvider = services.BuildServiceProvider();
        var endpoint = serviceProvider.GetRequiredService<RpcEndpoint>();
        await using var sendingStream = Console.OpenStandardOutput();
        await using var receivingStream = Console.OpenStandardInput();

        var rpc = JsonRpc.Attach(sendingStream, receivingStream, endpoint);
        endpoint.Attach(rpc);
        rpc.Disconnected += (_, disconnectedArgs) =>
        {
            if (disconnectedArgs.Exception != null)
                Console.Error.WriteLine(disconnectedArgs.Exception);
        };

        await rpc.Completion;
    }

    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        var jobsDbConnection = $"Data Source={Config.DatabasePath}";
        services.AddElsa(elsa =>
        {
            elsa.UseWorkflowManagement(management =>
                management.UseEntityFrameworkCore(ef => ef.UseSqlite(jobsDbConnection)));
            elsa.UseWorkflowRuntime(runtime =>
                runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(jobsDbConnection)));
        });
        services.AddSingleton<SettingManager>(_ =>
        {
            var configManager = new SettingManager();
            configManager.Load();
            return configManager;
        });
        services.AddSingleton<IConfigProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<SettingManager>());
        services.AddSingleton<JobSnapshotWorkflowDispatcher>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<IFaceDetectorModelFactory, IFaceDetectorModelFactory>();
        services.AddSingleton<FaceDetectorModelManager>();
        services.AddSingleton<IFaceDetectorModelProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FaceDetectorModelManager>());
        services.AddSingleton<IRegistry<IPresentation>, XmlSlideRegistry>();
        services.AddSingleton<ISlideContentOperator, XmlSlideContentReplacer>();
        services.AddSingleton<IRegistry<IReadOnlyWorkbook>, XlWorkbookRegistry>();
        services.AddSingleton<GenerateService>();
        services.AddSingleton<BackendService>();
        services.AddSingleton<RpcEndpoint>();

        return services;
    }
}
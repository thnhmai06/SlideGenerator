using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Ipc.Endpoints;
using SlideGenerator.Features.Configs.Contracts;
using SlideGenerator.Features.Configs.Entities;
using SlideGenerator.Features.Configs.Services;
using SlideGenerator.Features.Jobs;
using SlideGenerator.Framework.Features.Image.Contracts;
using SlideGenerator.Framework.Features.Image.Services;
using SlideGenerator.Services;
using SlideGenerator.Services.Generating.Services;
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
        services.AddSingleton<ConfigManager>(_ =>
        {
            var configManager = new ConfigManager();
            configManager.Load();
            return configManager;
        });
        services.AddSingleton<IConfigProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<ConfigManager>());
        services.AddSingleton<JobSnapshotWorkflowDispatcher>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<IFaceDetectorModelFactory, IFaceDetectorModelFactory>();
        services.AddSingleton<FaceDetectorModelManager>();
        services.AddSingleton<IFaceDetectorModelProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FaceDetectorModelManager>());
        services.AddSingleton<GenerateService>();
        services.AddSingleton<BackendService>();
        services.AddSingleton<RpcEndpoint>();

        return services;
    }
}
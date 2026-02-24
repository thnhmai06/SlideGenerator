using Microsoft.Extensions.DependencyInjection;
using Elsa.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using StreamJsonRpc;
using SlideGenerator.Configs.Contracts;
using SlideGenerator.Configs.Entities;
using SlideGenerator.Configs.Services;
using SlideGenerator.Generating.Services;
using SlideGenerator.Ipc.Endpoints;
using SlideGenerator.Jobs;

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
        services.AddSingleton<FaceDetectorModelManager>();
        services.AddSingleton<GenerateService>();
        services.AddSingleton<BackendService>();
        services.AddSingleton<RpcEndpoint>();

        return services;
    }
}
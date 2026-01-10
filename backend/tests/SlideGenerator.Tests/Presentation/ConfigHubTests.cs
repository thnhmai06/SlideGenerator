using Microsoft.Extensions.Logging.Abstractions;
using SlideGenerator.Application.Features.Configs;
using SlideGenerator.Application.Features.Configs.DTOs.Responses.Successes;
using SlideGenerator.Domain.Configs;
using SlideGenerator.Infrastructure.Features.Configs;
using SlideGenerator.Presentation.Features.Configs;
using SlideGenerator.Tests.Helpers;

namespace SlideGenerator.Tests.Presentation;

[TestClass]
[DoNotParallelize]
public sealed class ConfigHubTests
{
    [TestMethod]
    public async Task ProcessRequest_Get_ReturnsConfig()
    {
        var hub = CreateHub(out var proxy);
        var message = JsonHelper.Parse("{\"type\":\"get\"}");

        await hub.ProcessRequest(message);

        var response = proxy.GetPayload<ConfigGetSuccess>();
        Assert.IsNotNull(response);
        Assert.AreEqual(ConfigHolder.Value.Job.MaxConcurrentJobs, response.Job.MaxConcurrentJobs);
    }

    [TestMethod]
    public async Task ProcessRequest_Update_ChangesConfig()
    {
        var original = ConfigTestHelper.GetConfig();
        var originalDir = Environment.CurrentDirectory;
        var tempDir = CreateTempDirectory();

        try
        {
            Environment.CurrentDirectory = tempDir;
            var hub = CreateHub(out var proxy);
            var message = JsonHelper.Parse("{\"type\":\"update\",\"job\":{\"maxConcurrentJobs\":9}}");

            await hub.ProcessRequest(message);

            var response = proxy.GetPayload<ConfigUpdateSuccess>();
            Assert.IsNotNull(response);
            Assert.AreEqual(9, ConfigHolder.Value.Job.MaxConcurrentJobs);
        }
        finally
        {
            ConfigTestHelper.SetConfig(original);
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task ProcessRequest_Reload_LoadsFromDisk()
    {
        var original = ConfigTestHelper.GetConfig();
        var originalDir = Environment.CurrentDirectory;
        var tempDir = CreateTempDirectory();

        try
        {
            Environment.CurrentDirectory = tempDir;
            var @lock = new Lock();
            var saved = new Config
            {
                Job = new Config.JobConfig { MaxConcurrentJobs = 7 }
            };
            ConfigLoader.Save(saved, @lock);
            ConfigTestHelper.SetConfig(new Config
            {
                Job = new Config.JobConfig { MaxConcurrentJobs = 2 }
            });

            var hub = CreateHub(out var proxy);
            await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"reload\"}"));

            var response = proxy.GetPayload<ConfigReloadSuccess>();
            Assert.IsNotNull(response);
            Assert.AreEqual(7, ConfigHolder.Value.Job.MaxConcurrentJobs);
        }
        finally
        {
            ConfigTestHelper.SetConfig(original);
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task ProcessRequest_Reset_ResetsDefaults()
    {
        var original = ConfigTestHelper.GetConfig();
        var originalDir = Environment.CurrentDirectory;
        var tempDir = CreateTempDirectory();

        try
        {
            Environment.CurrentDirectory = tempDir;
            ConfigTestHelper.SetConfig(new Config
            {
                Job = new Config.JobConfig { MaxConcurrentJobs = 12 }
            });

            var hub = CreateHub(out var proxy);
            await hub.ProcessRequest(JsonHelper.Parse("{\"type\":\"reset\"}"));

            var response = proxy.GetPayload<ConfigResetSuccess>();
            Assert.IsNotNull(response);
            Assert.AreEqual(5, ConfigHolder.Value.Job.MaxConcurrentJobs);
        }
        finally
        {
            ConfigTestHelper.SetConfig(original);
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }

    private static ConfigHub CreateHub(out CaptureClientProxy proxy)
    {
        var hub = new ConfigHub(new FakeJobManager(new FakeActiveJobCollection()),
            NullLogger<ConfigHub>.Instance);
        proxy = HubTestHelper.Attach(hub, "conn-1");
        return hub;
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SlideGeneratorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }
}
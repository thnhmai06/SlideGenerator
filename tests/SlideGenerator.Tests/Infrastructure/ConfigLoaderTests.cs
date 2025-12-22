using SlideGenerator.Domain.Configs;
using SlideGenerator.Infrastructure.Configs;

namespace SlideGenerator.Tests.Infrastructure;

[TestClass]
[DoNotParallelize]
public sealed class ConfigLoaderTests
{
    [TestMethod]
    public void Load_ReturnsNullWhenMissing()
    {
        var originalDir = Environment.CurrentDirectory;
        var tempDir = Path.Combine(Path.GetTempPath(), "SlideGeneratorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            Environment.CurrentDirectory = tempDir;
            var @lock = new Lock();

            var loaded = ConfigLoader.Load(@lock);

            Assert.IsNull(loaded);
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public void SaveAndLoad_RoundTripsJobConfig()
    {
        var originalDir = Environment.CurrentDirectory;
        var tempDir = Path.Combine(Path.GetTempPath(), "SlideGeneratorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            Environment.CurrentDirectory = tempDir;
            var @lock = new Lock();
            var config = new Config
            {
                Job = new Config.JobConfig { MaxConcurrentJobs = 7 }
            };

            ConfigLoader.Save(config, @lock);
            var loaded = ConfigLoader.Load(@lock);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(7, loaded.Job.MaxConcurrentJobs);
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }
}
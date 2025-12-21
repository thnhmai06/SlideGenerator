using SlideGenerator.Domain.Configs;

namespace SlideGenerator.Tests.Domain;

[TestClass]
public sealed class JobConfigTests
{
    [TestMethod]
    public void Defaults_MaxConcurrentJobsIsFive()
    {
        var config = new Config.JobConfig();
        Assert.AreEqual(5, config.MaxConcurrentJobs);
    }
}

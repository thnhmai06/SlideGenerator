namespace SlideGenerator.Domain.Configs.Models;

public sealed partial class Config
{
    public static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "Jobs.db");

    public sealed class JobConfig
    {
        public int MaxConcurrentJobs = 5;
        public int MaxRetries = 3;
    }
}
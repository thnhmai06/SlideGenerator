namespace SlideGenerator.Application.Configs.Models;

public sealed partial class Config
{
    public sealed class JobConfig
    {
        public int MaxConcurrentJobs { get; init; } = 5;
    }
}
namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public sealed class JobConfig
    {
        public int MaxConcurrentJobs { get; init; } = 5;
    }
}
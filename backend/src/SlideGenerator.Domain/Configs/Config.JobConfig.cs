namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public sealed class JobConfig
    {
        public int MaxConcurrentJobs { get; init; } = 5;

        public string OutputFolder
        {
            get => string.IsNullOrWhiteSpace(field) ? DefaultTempPath : field;
            init;
        } = string.Empty;

        public string DatabasePath
        {
            get => string.IsNullOrWhiteSpace(field)
                ? Path.Combine(DefaultTempPath, "jobs.db")
                : field;
            init;
        } = string.Empty;
    }
}
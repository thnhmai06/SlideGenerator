namespace SlideGenerator.Application.Configs.Models;

public sealed partial class Config
{
    public sealed class JobConfig
    {
        public int MaxConcurrentJobs { get; init; } = 2;

        public string OutputFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultOutputPath : field;
            init;
        } = string.Empty;

        public string DatabasePath
        {
            get => string.IsNullOrEmpty(field) ? DefaultJobsDatabasePath : field;
            init;
        } = string.Empty;
    }
}
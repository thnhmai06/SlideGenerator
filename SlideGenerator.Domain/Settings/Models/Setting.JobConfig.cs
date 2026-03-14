namespace SlideGenerator.Domain.Settings.Models;

public sealed partial class Setting
{
    public static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "Jobs.db");

    public sealed class JobSetting
    {
        public int MaxConcurrentJobs = 5;
        public int MaxRetries = 3;
    }
}
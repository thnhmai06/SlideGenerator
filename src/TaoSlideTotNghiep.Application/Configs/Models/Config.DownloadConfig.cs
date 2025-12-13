namespace TaoSlideTotNghiep.Application.Configs.Models;

public sealed partial class Config
{
    public sealed class DownloadConfig
    {
        public class RetryConfig
        {
            public int Timeout { get; init; } = 30;
            public int MaxRetries { get; init; } = 3;
        }

        public int MaxChunks { get; init; } = 5;
        public int LimitBytesPerSecond { get; init; } = 0;

        public string SaveFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultTempPath : field;
            init;
        } = string.Empty;

        public RetryConfig Retry { get; init; } = new();
    }
}
using System.Net;

namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public sealed class DownloadConfig
    {
        public int MaxChunks { get; init; } = 5;
        public int LimitBytesPerSecond { get; init; } = 0;

        public string SaveFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultTempPath : field;
            init;
        } = string.Empty;

        public RetryConfig Retry { get; init; } = new();

        public ProxyConfig Proxy { get; init; } = new();

        public class RetryConfig
        {
            public int Timeout { get; init; } = 30;
            public int MaxRetries { get; init; } = 3;
        }

        public class ProxyConfig
        {
            public bool UseProxy { get; init; } = false;
            public string ProxyAddress { get; init; } = string.Empty;
            public string Username { get; init; } = string.Empty;
            public string Password { get; init; } = string.Empty;
            public string Domain { get; init; } = string.Empty;

            public IWebProxy? GetWebProxy()
            {
                if (!UseProxy || string.IsNullOrEmpty(ProxyAddress))
                    return null;

                var proxy = new WebProxy(ProxyAddress)
                {
                    Credentials = new NetworkCredential(Username, Password, Domain)
                };
                return proxy;
            }
        }
    }
}
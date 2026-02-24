using System.Net;

namespace SlideGenerator.Configs.Entities;

public sealed partial class Config
{
    private static readonly string DefaultDownloadPath = Path.Combine(Path.GetTempPath(), AppName);

    public sealed class DownloadConfig
    {
        public bool DeleteAfterDownload = true;
        public int LimitBytesPerSecond = 0;
        public int MaxChunks = 5;

        public ProxyConfig Proxy = new();

        public RetryConfig Retry = new();

        public string SaveFolder
        {
            get => string.IsNullOrEmpty(field) ? DefaultDownloadPath : field;
            set;
        } = string.Empty;

        public sealed class RetryConfig
        {
            public int MaxRetries = 3;
            public int Timeout = 30;
        }

        public sealed class ProxyConfig
        {
            public string Domain = string.Empty;
            public string Password = string.Empty;
            public string ProxyAddress = string.Empty;
            public bool UseProxy = false;
            public string Username = string.Empty;

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
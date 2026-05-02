using System.Net;
using SlideGenerator.Settings.Rules;

namespace SlideGenerator.Settings.Settings;

public sealed partial class Setting
{
    public sealed class DownloadSetting
    {
        public readonly TempSetting Temp = new();
        public readonly ProxySetting Proxy = new();
        public readonly RetrySetting Retry = new();

        public sealed class TempSetting
        {
            public string FolderPath
            {
                get => string.IsNullOrEmpty(field) ? NameAndPathRules.DefaultTempPath : field;
                set
                {
                    if (!string.IsNullOrEmpty(value) && !Directory.Exists(value))
                        Directory.CreateDirectory(value);
                    field = value;
                }
            } = string.Empty;

            public bool DeleteDownload { get; set; } = false;
            public bool DeleteEdit { get; set; } = true;
        }

        public sealed class RetrySetting
        {
            public int MaxRetries { get; set; } = 3;
            public int Timeout { get; set; } = 30;
        }

        public sealed class ProxySetting
        {
            public bool UseProxy { get; set; } = false;
            public string Domain { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ProxyAddress { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;

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
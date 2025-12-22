namespace SlideGenerator.Domain.Configs;

public sealed partial class Config
{
    public sealed class ServerConfig
    {
        public string Host { get; init; } = "127.0.0.1";
        public int Port { get; init; } = 65550;
        public bool Debug { get; init; } = false;
    }
}

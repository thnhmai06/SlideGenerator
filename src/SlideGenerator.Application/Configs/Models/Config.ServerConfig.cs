namespace SlideGenerator.Application.Configs.Models;

public sealed partial class Config
{
    public sealed class ServerConfig
    {
        public string Host { get; init; } = "127.0.0.1";
        public int Port { get; init; } = 5000;
        public bool Debug { get; init; } = false;
    }
}
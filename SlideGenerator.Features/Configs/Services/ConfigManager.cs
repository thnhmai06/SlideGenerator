using SlideGenerator.Features.Configs.Contracts;
using SlideGenerator.Features.Configs.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlideGenerator.Features.Configs.Services;

/// <remarks>
/// Reviewed by @thnhmai06 at 01/03/2026 00:43:30 GMT+7
/// </remarks>
public sealed class ConfigManager : IConfigProvider
{
    private static readonly string ConfigFilePath = Path.Combine(AppContext.BaseDirectory, "Configs.yaml");

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public Config Current { get; private set; } = new();

    public bool Load()
    {
        try
        {
            var yaml = File.ReadAllText(ConfigFilePath);
            var loaded = _deserializer.Deserialize<Config>(yaml);

            Current = loaded;
            return true;
        }
        catch
        {
            // TODO: log the error
        }

        return false;
    }

    public bool Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetPathRoot(ConfigFilePath)!);
            File.WriteAllText(ConfigFilePath, _serializer.Serialize(Current));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ResetToDefaults()
    {
        Current = new Config();
        return Save();
    }
}
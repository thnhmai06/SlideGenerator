using SlideGenerator.Configs.Contracts;
using SlideGenerator.Configs.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlideGenerator.Configs.Services;

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
        foreach (var candidate in EnumerateCandidates(ConfigFilePath))
            try
            {
                if (!File.Exists(candidate)) continue;
                var yaml = File.ReadAllText(candidate);
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
        var target = ResolveSavePath(ConfigFilePath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target) ?? AppContext.BaseDirectory);
            File.WriteAllText(target, _serializer.Serialize(Current));
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

    private static IEnumerable<string> EnumerateCandidates(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return explicitPath;

        yield return ConfigFilePath;
    }

    private static string ResolveSavePath(string? explicitPath)
    {
        return !string.IsNullOrWhiteSpace(explicitPath)
            ? explicitPath
            : ConfigFilePath;
    }
}
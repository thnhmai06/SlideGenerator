using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TaoSlideTotNghiep.Infrastructure.Config;

public static class ConfigManager
{
    private const string ConfigFileName = "backend.config.yaml";

    private static readonly Lazy<ConfigModel> LazyInstance = new(() => new ConfigModel());
    public static ConfigModel Value => LazyInstance.Value;

    /// <summary>
    /// Loads/Reloads configuration from the YAML file.
    /// </summary>
    public static void Load()
    {
        if (!File.Exists(ConfigFileName))
        {
            Save();
            return;
        }

        try
        {
            var yaml = File.ReadAllText(ConfigFileName);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var loaded = deserializer.Deserialize<ConfigModel>(yaml);
            Value.Server = loaded.Server;
            Value.Download = loaded.Download;
        }
        catch
        {
            ResetToDefaults();
        }
    }

    /// <summary>
    /// Saves current configuration to the YAML file.
    /// </summary>
    public static void Save()
    {
        var directory = Path.GetDirectoryName(ConfigFileName);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(Value);
        File.WriteAllText(ConfigFileName, yaml);
    }


    /// <summary>
    /// Resets configuration to default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        Value.Server = new ConfigModel.ServerConfig();
        Value.Download = new ConfigModel.DownloadConfig();
        Save();
    }
}
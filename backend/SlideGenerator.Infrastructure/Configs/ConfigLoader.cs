using SlideGenerator.Application.Configs.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlideGenerator.Infrastructure.Configs;

public static class ConfigLoader
{
    /// <summary>
    ///     Loads/Reloads configuration.
    /// </summary>
    /// <param name="lock">A lock object used to synchronize access during the operation.</param>
    public static Config? Load(Lock @lock)
    {
        lock (@lock)
        {
            if (File.Exists(Config.FileName))
                try
                {
                    var yaml = File.ReadAllText(Config.FileName);
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(UnderscoredNamingConvention.Instance)
                        .IgnoreUnmatchedProperties()
                        .Build();

                    return deserializer.Deserialize<Config>(yaml);
                }
                catch
                {
                    // TODO: Log Error
                }

            return null;
        }
    }

    /// <summary>
    ///     Saves current configuration to the YAML file.
    /// </summary>
    /// <param name="config">The configuration object to save.</param>
    /// <param name="lock">A lock object used to synchronize access during the operation.</param>
    public static void Save(Config config, Lock @lock)
    {
        lock (@lock)
        {
            var directory = Path.GetDirectoryName(Config.FileName);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(config);
            File.WriteAllText(Config.FileName, yaml);
        }
    }
}
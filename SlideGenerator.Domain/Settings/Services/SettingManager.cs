using SlideGenerator.Domain.Settings.Contacts;
using SlideGenerator.Domain.Settings.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlideGenerator.Domain.Settings.Services;

/// <remarks>
///     Reviewed by @thnhmai06 at 01/03/2026 00:43:30 GMT+7
/// </remarks>
public sealed class SettingManager : ISettingProvider
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "Settings.yaml");

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public Setting Current { get; private set; } = new();

    public bool Load()
    {
        try
        {
            var yaml = File.ReadAllText(FilePath);
            var loaded = _deserializer.Deserialize<Setting>(yaml);

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
            Directory.CreateDirectory(Path.GetPathRoot(FilePath)!);
            File.WriteAllText(FilePath, _serializer.Serialize(Current));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ResetToDefaults()
    {
        Current = new Setting();
        return Save();
    }
}
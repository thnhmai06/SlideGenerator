using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ISerializer = SlideGenerator.Application.Settings.Abstractions.ISerializer;

namespace SlideGenerator.Infrastructure.Settings.Services;

/// <summary>
///     Provides YAML serialization and deserialization using YamlDotNet.
/// </summary>
public class YamlSerializer : ISerializer
{
    /// <summary>
    ///     The internal YamlDotNet deserializer configured with underscored naming conventions.
    /// </summary>
    private readonly IDeserializer _deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    /// <summary>
    ///     The internal YamlDotNet serializer configured with underscored naming conventions.
    /// </summary>
    private readonly YamlDotNet.Serialization.ISerializer _serializer =
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

    /// <inheritdoc />
    public string FileExtension => ".yaml";

    /// <inheritdoc />
    public T Deserialize<T>(string input)
    {
        return _deserializer.Deserialize<T>(input);
    }

    /// <inheritdoc />
    public string Serialize(object? graph)
    {
        return _serializer.Serialize(graph);
    }
}
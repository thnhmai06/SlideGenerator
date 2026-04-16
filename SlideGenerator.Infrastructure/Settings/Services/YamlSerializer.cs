using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ISerializer = SlideGenerator.Application.Settings.Abstractions.ISerializer;

namespace SlideGenerator.Infrastructure.Settings.Services;

public class YamlSerializer : ISerializer
{
    private readonly IDeserializer _deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private readonly YamlDotNet.Serialization.ISerializer _serializer =
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

    public string FileExtension => ".yaml";

    public T Deserialize<T>(string input)
    {
        return _deserializer.Deserialize<T>(input);
    }

    public string Serialize(object? graph)
    {
        return _serializer.Serialize(graph);
    }
}
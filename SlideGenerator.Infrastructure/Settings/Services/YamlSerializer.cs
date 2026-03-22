using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlideGenerator.Infrastructure.Settings.Services;

public class YamlSerializer : SlideGenerator.Application.Settings.Abstractions.ISerializer
{
    public string FileExtension => ".yaml";

    private readonly IDeserializer _deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private readonly ISerializer _serializer =
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

    public T Deserialize<T>(string input)
    {
        return _deserializer.Deserialize<T>(input);
    }

    public string Serialize(object? graph)
    {
        return _serializer.Serialize(graph);
    }
}
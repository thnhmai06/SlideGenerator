using SlideGenerator.Application.Settings.Abstractions;

namespace SlideGenerator.Infrastructure.Settings.Adapters;

public class YamlSerializer : ISerializer
{
    public string FileExtension => ".yaml";

    private readonly YamlDotNet.Serialization.IDeserializer _deserializer =
        new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private readonly YamlDotNet.Serialization.ISerializer _serializer =
        new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
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
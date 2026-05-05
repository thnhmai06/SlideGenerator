using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SlideGenerator.Settings.Entities;

/// <summary>
///     Implements the <see cref="Serializer" /> abstraction using the YAML format.
///     Uses CamelCase naming conventions and ignores unmatched properties during deserialization.
/// </summary>
public class YamlSerializer : Serializer
{
    /// <inheritdoc />
    public override string FileExtension => ".yaml";

    /// <inheritdoc />
    public override T Deserialize<T>(string source)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<T>(source);
    }

    /// <inheritdoc />
    public override string Serialize<T>(T obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(obj);
    }
}
namespace SlideGenerator.Settings.Entities;

public class YamlSerializer : Serializer
{
    public override string FileExtension => ".yaml";

    public override T Deserialize<T>(string source)
    {
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<T>(source);
    }

    public override string Serialize<T>(T obj)
    {
        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(obj);
    }
}
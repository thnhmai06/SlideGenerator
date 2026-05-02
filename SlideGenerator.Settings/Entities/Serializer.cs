namespace SlideGenerator.Settings.Entities;

public abstract class Serializer
{
    public abstract string FileExtension { get; }

    public abstract string Serialize<T>(T obj);

    public abstract T Deserialize<T>(string source);
}
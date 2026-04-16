namespace SlideGenerator.Application.Settings.Abstractions;

public interface ISerializer
{
    string FileExtension { get; }

    T Deserialize<T>(string input);

    /// <summary>
    ///     Serializes the specified object into a string.
    /// </summary>
    /// <param name="graph">The object to serialize.</param>
    string Serialize(object? graph);
}
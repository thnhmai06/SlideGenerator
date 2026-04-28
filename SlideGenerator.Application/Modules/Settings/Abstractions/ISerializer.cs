namespace SlideGenerator.Application.Modules.Settings.Abstractions;

/// <summary>
///     Defines a contract for serializing and deserializing application settings and objects.
/// </summary>
public interface ISerializer
{
    /// <summary>Gets the default file extension associated with this serializer format (e.g., ".json").</summary>
    string FileExtension { get; }

    /// <summary>
    ///     Deserializes the provided string input into an object of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of the object to deserialize into.</typeparam>
    /// <param name="input">The serialized string data.</param>
    /// <returns>The deserialized object.</returns>
    T Deserialize<T>(string input);

    /// <summary>
    ///     Serializes the specified object into a string.
    /// </summary>
    /// <param name="graph">The object graph to serialize.</param>
    /// <returns>A string representation of the serialized object.</returns>
    string Serialize(object? graph);
}
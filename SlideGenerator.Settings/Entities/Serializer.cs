namespace SlideGenerator.Settings.Entities;

/// <summary>
///     Provides an abstraction for serializing and deserializing objects.
/// </summary>
public abstract class Serializer
{
    /// <summary>
    ///     Gets the standard file extension (e.g., .json, .yaml) associated with this serializer.
    /// </summary>
    public abstract string FileExtension { get; }

    /// <summary>
    ///     Serializes an object into its string representation.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A string representation of the object.</returns>
    public abstract string Serialize<T>(T obj);

    /// <summary>
    ///     Deserializes a string into an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type of the deserialized object.</typeparam>
    /// <param name="source">The string representation to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    public abstract T Deserialize<T>(string source);
}
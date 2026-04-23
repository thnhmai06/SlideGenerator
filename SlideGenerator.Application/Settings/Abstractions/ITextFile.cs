namespace SlideGenerator.Application.Settings.Abstractions;

/// <summary>
///     Represents an abstraction over a physical text file for reading and writing contents.
/// </summary>
public interface ITextFile : IDisposable
{
    /// <summary>Gets the absolute or relative file path to the text file.</summary>
    string FilePath { get; }

    /// <summary>
    ///     Reads the entire content of the text file as a single string.
    /// </summary>
    /// <returns>The content of the file.</returns>
    string Read();

    /// <summary>
    ///     Writes the specified string content to the text file, overwriting existing content.
    /// </summary>
    /// <param name="content">The string content to write.</param>
    void Write(string? content);
}

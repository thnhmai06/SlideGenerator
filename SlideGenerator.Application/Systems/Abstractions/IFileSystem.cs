namespace SlideGenerator.Application.Systems.Abstractions;

/// <summary>
///     Provides an abstraction for fundamental file system operations.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    ///     Copies an existing file to a new file.
    /// </summary>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    /// <param name="overwrite">
    ///     <see langword="true" /> if the destination file can be overwritten; otherwise,
    ///     <see langword="false" />.
    /// </param>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = true);
}
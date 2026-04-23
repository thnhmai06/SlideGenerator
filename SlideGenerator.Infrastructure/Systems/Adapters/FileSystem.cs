using SlideGenerator.Application.Systems.Abstractions;

namespace SlideGenerator.Infrastructure.Systems.Adapters;

/// <summary>
/// Provides a thin wrapper around the system file operations.
/// </summary>
public class FileSystem : IFileSystem
{
    /// <summary>
    /// Copies an existing file to a new file.
    /// </summary>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destinationPath">The path to the new file.</param>
    /// <param name="overwrite"><see langword="true" /> to overwrite the destination file if it exists; otherwise, <see langword="false" />.</param>
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = true)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
    }
}
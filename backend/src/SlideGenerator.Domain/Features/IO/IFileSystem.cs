namespace SlideGenerator.Domain.Features.IO;

/// <summary>
///     Provides filesystem operations for job orchestration.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    ///     Checks whether a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    ///     Copies a file to the destination.
    /// </summary>
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>
    ///     Deletes a file if it exists.
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    ///     Ensures a directory exists.
    /// </summary>
    void EnsureDirectory(string path);
}
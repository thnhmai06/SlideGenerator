using SlideGenerator.Domain.Features.IO;

namespace SlideGenerator.Infrastructure.Features.IO;

/// <summary>
///     File system implementation using System.IO.
/// </summary>
public sealed class FileSystem : IFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc />
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
    }

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <inheritdoc />
    public void EnsureDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path);
    }
}
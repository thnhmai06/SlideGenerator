using SlideGenerator.Application.Systems.Abstractions;

namespace SlideGenerator.Infrastructure.Systems.Adapter;

public class FileSystem : IFileSystem
{
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = true)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
    }
}
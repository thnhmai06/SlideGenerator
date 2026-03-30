using SlideGenerator.Application.System;

namespace SlideGenerator.Infrastructure.System;

public class FileSystem : IFileSystem
{
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = true)
    {
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }
}
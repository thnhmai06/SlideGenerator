namespace SlideGenerator.Application.System;

public interface IFileSystem
{
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = true);
}
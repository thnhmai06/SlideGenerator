namespace SlideGenerator.Application.Systems.Abstractions;

public interface IFileSystem
{
    void CopyFile(string sourcePath, string destinationPath, bool overwrite = true);
}